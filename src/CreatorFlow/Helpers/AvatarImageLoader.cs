using System.Net.Http;
using System.Runtime.InteropServices;

namespace CreatorFlow.Helpers;

internal static class AvatarImageLoader
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static Image? Decode(byte[] data)
    {
        if (data.Length is 0 or > MaximumBytes) return null;
        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using Image image = Image.FromStream(stream, false, true);
            return image.Width > 512 || image.Height > 512 ? null : new Bitmap(image);
        }
        catch (Exception exception) when (exception is ArgumentException or ExternalException or OutOfMemoryException)
        { return null; }
    }

    public static async Task<Image?> LoadAsync(string? url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using HttpResponseMessage response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaximumBytes) return null;
            await using Stream source = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var data = new MemoryStream();
            byte[] buffer = new byte[8192];
            int count;
            while ((count = await source.ReadAsync(buffer, deadline.Token)) > 0)
            {
                if (data.Length + count > MaximumBytes) return null;
                data.Write(buffer, 0, count);
            }
            data.Position = 0;
            using Image decoded = Image.FromStream(data, false, true);
            // Bound decoded dimensions before allocating a second bitmap.
            if (decoded.Width > 4096 || decoded.Height > 4096) return null;
            return new Bitmap(decoded);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (Exception exception) when (exception is HttpRequestException or IOException or
            ArgumentException or ExternalException or OutOfMemoryException) { return null; }
    }
}
