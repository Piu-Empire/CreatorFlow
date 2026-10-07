using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace CreatorFlow.Api.Tests.Services;

internal static class AvatarFixtures
{
    public static readonly byte[] Marker = Encoding.ASCII.GetBytes("Synthetic source-private metadata fixture");
    public static byte[] Encode(int width, int height, bool jpeg = false)
    {
        using var pixels = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(pixels))
        {
            canvas.Clear(SKColors.Red);
            using var paint = new SKPaint();
            paint.Color = SKColors.Green; canvas.DrawRect(width / 2f, 0, width / 2f, height / 2f, paint);
            paint.Color = SKColors.Blue; canvas.DrawRect(0, height / 2f, width / 2f, height / 2f, paint);
            paint.Color = SKColors.Yellow; canvas.DrawRect(width / 2f, height / 2f, width / 2f, height / 2f, paint);
        }
        using var image = SKImage.FromBitmap(pixels);
        using var data = image.Encode(jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 95);
        return data.ToArray();
    }

    public static byte[] WithMetadata(byte[] input, bool jpeg, ushort orientation = 1)
    {
        using var data = new MemoryStream();
        if (jpeg)
        {
            using var writer = new BinaryWriter(data, Encoding.ASCII, leaveOpen: true);
            writer.Write("Exif\0\0"u8); writer.Write("II"u8); writer.Write((ushort)42); writer.Write(8u);
            writer.Write((ushort)2);
            writer.Write((ushort)0x010e); writer.Write((ushort)2); writer.Write((uint)Marker.Length + 1); writer.Write(38u);
            writer.Write((ushort)0x0112); writer.Write((ushort)3); writer.Write(1u); writer.Write(orientation); writer.Write((ushort)0);
            writer.Write(0u); writer.Write(Marker); writer.Write((byte)0);
        }
        else { data.Write("tEXtDescription\0"u8); data.Write(Marker); }
        byte[] payload = data.ToArray();
        using var result = new MemoryStream();
        int offset = jpeg ? 2 : 33;
        result.Write(input.AsSpan(0, offset));
        if (jpeg)
        {
            result.Write([255, 225]); Span<byte> length = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)(payload.Length + 2)));
            result.Write(length); result.Write(payload);
        }
        else
        {
            Span<byte> number = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(number, (uint)payload.Length - 4);
            result.Write(number); result.Write(payload);
            uint crc = uint.MaxValue;
            foreach (byte value in payload)
            {
                crc ^= value;
                for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xedb88320u : 0u);
            }
            BinaryPrimitives.WriteUInt32BigEndian(number, ~crc); result.Write(number);
        }
        result.Write(input.AsSpan(offset));
        return result.ToArray();
    }
}
