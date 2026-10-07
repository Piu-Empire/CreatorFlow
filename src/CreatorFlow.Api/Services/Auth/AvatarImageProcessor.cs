using CreatorFlow.Api.Models.Auth;
using SkiaSharp;

namespace CreatorFlow.Api.Services.Auth;

public sealed class AvatarImageProcessor : IAvatarImageProcessor
{
    public const int MaximumBytes = 2 * 1024 * 1024;
    public UserAvatar Normalize(byte[] input)
    {
        if (input.Length is 0 or > MaximumBytes) throw InvalidImage();
        bool png = input.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        bool jpeg = input.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
        if (!png && !jpeg) throw InvalidImage();
        using var data = SKData.CreateCopy(input);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.Info.Width is <= 0 or > 4096 || codec.Info.Height is <= 0 or > 4096 ||
            (png && codec.EncodedFormat != SKEncodedImageFormat.Png) || (jpeg && codec.EncodedFormat != SKEncodedImageFormat.Jpeg)) throw InvalidImage();

        // Read dimensions before allocating decoded pixels; tolerate neither truncated nor partial decoding.
        var sourceInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var pixels = new SKBitmap(sourceInfo);
        if (codec.GetPixels(sourceInfo, pixels.GetPixels()) != SKCodecResult.Success) throw InvalidImage();
        bool swapsAxes = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        int orientedWidth = swapsAxes ? sourceInfo.Height : sourceInfo.Width;
        int orientedHeight = swapsAxes ? sourceInfo.Width : sourceInfo.Height;
        double scale = Math.Min(1, 512d / Math.Max(orientedWidth, orientedHeight));
        int width = Math.Max(1, (int)(orientedWidth * scale));
        int height = Math.Max(1, (int)(orientedHeight * scale));
        using var output = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.Transparent);
            var matrix = OriginMatrix(codec.EncodedOrigin, sourceInfo.Width, sourceInfo.Height,
                (float)width / orientedWidth, (float)height / orientedHeight);
            canvas.SetMatrix(in matrix);
            using var image = SKImage.FromBitmap(pixels);
            canvas.DrawImage(image, new SKRect(0, 0, sourceInfo.Width, sourceInfo.Height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        }
        // Fresh pixels and PNG encoding carry no source EXIF/text/profile metadata.
        using var normalized = SKImage.FromBitmap(output);
        using var encoded = normalized.Encode(SKEncodedImageFormat.Png, 100);
        if (encoded is null || encoded.Size > MaximumBytes) throw InvalidImage();
        return new(encoded.ToArray(), width, height);
    }

    private static SKMatrix OriginMatrix(SKEncodedOrigin origin, int width, int height, float sx, float sy)
    {
        (float a, float b, float c, float d, float e, float f) = origin switch
        {
            SKEncodedOrigin.TopRight => (-1, 0, width, 0, 1, 0),
            SKEncodedOrigin.BottomRight => (-1, 0, width, 0, -1, height),
            SKEncodedOrigin.BottomLeft => (1, 0, 0, 0, -1, height),
            SKEncodedOrigin.LeftTop => (0, 1, 0, 1, 0, 0),
            SKEncodedOrigin.RightTop => (0, -1, height, 1, 0, 0),
            SKEncodedOrigin.RightBottom => (0, -1, height, -1, 0, width),
            SKEncodedOrigin.LeftBottom => (0, 1, 0, -1, 0, width),
            _ => (1, 0, 0, 0, 1, 0)
        };
        return new(a * sx, b * sx, c * sx, d * sy, e * sy, f * sy, 0, 0, 1);
    }
    private static ArgumentException InvalidImage() => new("Ảnh không hợp lệ. Chọn JPEG/PNG tối đa 2 MiB và mỗi chiều tối đa 4096px.");
}
