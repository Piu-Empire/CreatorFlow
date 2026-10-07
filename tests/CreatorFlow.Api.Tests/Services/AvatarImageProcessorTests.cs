using CreatorFlow.Api.Services.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CreatorFlow.Api.Tests.Services;

[TestClass]
[TestCategory("AuthAvatarUnit")]
public sealed class AvatarImageProcessorTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SourceMetadata_IsDiscarded_WithCorrectFormatAndAspectRatio(bool jpeg)
    {
        byte[] input = AvatarFixtures.WithMetadata(AvatarFixtures.Encode(1024, 512, jpeg), jpeg);
        Assert.IsTrue(input.AsSpan().IndexOf(AvatarFixtures.Marker) >= 0);
        using var sourceData = SKData.CreateCopy(input); using var source = SKCodec.Create(sourceData);
        Assert.IsNotNull(source); Assert.AreEqual(1024, source.Info.Width); Assert.AreEqual(512, source.Info.Height);
        var avatar = new AvatarImageProcessor().Normalize(input);
        Assert.AreEqual(512, avatar.Width); Assert.AreEqual(256, avatar.Height);
        Assert.IsTrue(avatar.ImageData.Length <= AvatarImageProcessor.MaximumBytes);
        Assert.IsTrue(avatar.ImageData.AsSpan().IndexOf(AvatarFixtures.Marker) < 0);
        using var outputData = SKData.CreateCopy(avatar.ImageData); using var output = SKCodec.Create(outputData);
        Assert.AreEqual(SKEncodedImageFormat.Png, output.EncodedFormat);
        Assert.AreEqual(SKEncodedOrigin.TopLeft, output.EncodedOrigin);
        using var pixels = SKBitmap.Decode(avatar.ImageData);
        Assert.IsTrue(pixels.GetPixel(64, 64).Red > pixels.GetPixel(64, 64).Blue);
    }

    [TestMethod]
    [DataRow(1, false, "Red")]
    [DataRow(2, false, "Green")]
    [DataRow(3, false, "Yellow")]
    [DataRow(4, false, "Blue")]
    [DataRow(5, true, "Red")]
    [DataRow(6, true, "Blue")]
    [DataRow(7, true, "Yellow")]
    [DataRow(8, true, "Green")]
    public void ExifOrientation_IsAppliedBeforeMetadataRemoval(int orientation, bool swapsAxes, string expectedColor)
    {
        byte[] input = AvatarFixtures.WithMetadata(AvatarFixtures.Encode(64, 32, true), true, (ushort)orientation);
        using var sourceData = SKData.CreateCopy(input); using var source = SKCodec.Create(sourceData);
        Assert.AreEqual(orientation, (int)source.EncodedOrigin);
        var avatar = new AvatarImageProcessor().Normalize(input);
        Assert.AreEqual(swapsAxes ? 32 : 64, avatar.Width); Assert.AreEqual(swapsAxes ? 64 : 32, avatar.Height);
        using var decoded = SKBitmap.Decode(avatar.ImageData);
        SKColor actual = decoded.GetPixel(decoded.Width / 4, decoded.Height / 4);
        SKColor expected = expectedColor switch { "Red" => SKColors.Red, "Green" => SKColors.Green, "Blue" => SKColors.Blue, _ => SKColors.Yellow };
        Assert.IsTrue(Math.Abs(actual.Red - expected.Red) < 40 && Math.Abs(actual.Green - expected.Green) < 40 && Math.Abs(actual.Blue - expected.Blue) < 40);
        Assert.IsTrue(avatar.ImageData.AsSpan().IndexOf(AvatarFixtures.Marker) < 0);
    }

    [TestMethod]
    public void SpoofedEmptyOversizedAndExcessiveDimensions_AreRejected_AndSmallImageIsNotUpscaled()
    {
        var processor = new AvatarImageProcessor();
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize([]));
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize(new byte[2097153]));
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize([137,80,78,71,13,10,26,10]));
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize("not an image"u8.ToArray()));
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize(AvatarFixtures.Encode(4097, 1)));
        byte[] png = AvatarFixtures.Encode(16, 8);
        Assert.ThrowsExactly<ArgumentException>(() => processor.Normalize(png[..(png.Length / 2)]));
        var small = processor.Normalize(png); Assert.AreEqual(16, small.Width); Assert.AreEqual(8, small.Height);
    }
}
