namespace NadekoBot.Services;

public sealed class ImageCache : IImageCache, INService
{
    private readonly IBotCache _cache;
    private readonly ImagesConfig _ic;
    private readonly NadekoRandom _rng;
    private readonly IHttpClientFactory _httpFactory;

    public ImageCache(
        IBotCache cache,
        ImagesConfig ic,
        IHttpClientFactory httpFactory)
    {
        _cache = cache;
        _ic = ic;
        _httpFactory = httpFactory;
        _rng = new NadekoRandom();
    }

    private static TypedKey<byte[]> GetImageKey(Uri url)
        => new($"image:{url}");

    public async Task<byte[]?> GetImageDataAsync(Uri url)
        => await _cache.GetOrAddAsync(
            GetImageKey(url),
            async () =>
            {
                if (url.IsFile)
                {
                    return await File.ReadAllBytesAsync(url.LocalPath);
                }

                try
                {
                    using var http = _httpFactory.CreateClient();
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                    var bytes = await http.GetByteArrayAsync(url);
                    if (bytes is null || bytes.Length == 0)
                        return null;
                    return bytes;
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed downloading image {Url}: {Message}", url, ex.Message);
                    return null;
                }
            },
            expiry: TimeSpan.FromHours(48));

    private async Task<byte[]?> GetRandomImageDataAsync(Uri[] urls)
    {
        if (urls.Length == 0)
            return null;

        var url = urls[_rng.Next(0, urls.Length)];

        var data = await GetImageDataAsync(url);
        return data;
    }

    public Task<byte[]?> GetHeadsImageAsync()
        => GetRandomImageDataAsync(_ic.Data.Coins.Heads);

    public Task<byte[]?> GetTailsImageAsync()
        => GetRandomImageDataAsync(_ic.Data.Coins.Tails);

    public async Task<byte[]?> GetCurrencyImageAsync()
    {
        var data = await GetRandomImageDataAsync(_ic.Data.Currency);
        if (data is not null && data.Length > 0)
            return data;

        var localPath = Path.Combine("data", "currency.png");
        if (File.Exists(localPath))
        {
            try
            {
                return await File.ReadAllBytesAsync(localPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed loading fallback currency image: {Message}", ex.Message);
            }
        }

        return null;
    }

    public Task<byte[]?> GetXpBackgroundImageAsync()
        => GetImageDataAsync(_ic.Data.Xp.Bg);

    public Task<byte[]?> GetDiceAsync(int num)
        => GetImageDataAsync(_ic.Data.Dice[num]);

    public Task<byte[]?> GetSlotEmojiAsync(int number)
        => GetImageDataAsync(_ic.Data.Slots.Emojis[number]);

    public Task<byte[]?> GetSlotBgAsync()
        => GetImageDataAsync(_ic.Data.Slots.Bg);
}