using System.Net;
using Yukari.Core.Models;
using Yukari.Core.Sources;

namespace Yukari.Plugin.MangaFire;

[ComicSourceMetadata(
    "MangaFire",
    "0.1.0-Alpha+core2.4.0",
    "https://github.com/Yukari-App/Plugin.MangaFire/releases",
    "https://mangafire.to/assets/mangafire/logo-sm.png",
    "Access a vast array of manga in different languages."
)]
public class MangaFireSource : IComicSource, IRequiresHttpClient
{
    private static IReadOnlyList<Filter>? _filters;
    private static IReadOnlyDictionary<string, string>? _languages;

    public IReadOnlyList<Filter> Filters => _filters ??= [];

    public IReadOnlyDictionary<string, string> Languages =>
        _languages ??= new Dictionary<string, string>
        {
            { "en", "English" },
            { "fr", "Français" },
            { "es", "Español" },
            { "es-la", "Español LA" },
            { "pt", "Português (EU)" },
            { "pt-br", "Português" },
            { "ja", "日本語" },
        };

    private const string BaseUrl = "https://mangafire.to/api";

    private ISharedHttpClient? _httpClient;

    public void SetHttpClient(ISharedHttpClient httpClient) => _httpClient = httpClient;

    public Task<IReadOnlyList<Comic>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ChapterPage>> GetChapterPagesAsync(
        string comicId,
        string chapterId,
        CancellationToken ct = default
    )
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]?> GetImageBytesAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        request.Headers.Add("Referer", "https://mangafire.to");

        using var response = await _httpClient!.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "MangaFire Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
