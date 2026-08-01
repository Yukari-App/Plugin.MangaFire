using System.Net;
using System.Net.Http.Json;
using Yukari.Core.Models;
using Yukari.Core.Sources;
using Yukari.Plugin.MangaFire.Data;
using Yukari.Plugin.MangaFire.Helpers;

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
    private const int DefaultPageSize = 24;

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

    public async Task<IReadOnlyList<Comic>> SearchAsync(
        string query,
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        var queryParams = new Dictionary<string, string[]>
        {
            ["page"] = [page.ToString()],
            ["limit"] = [DefaultPageSize.ToString()],
        };

        if (!string.IsNullOrWhiteSpace(query))
            queryParams["keyword"] = [query];

        foreach (var kvp in filters)
            queryParams[kvp.Key] = kvp.Value.ToArray();

        var searchResults = (
            await GetFromApiAsync<SearchResponse>("/titles", queryParams, ct)
        )?.Comics;
        if (searchResults is not { Length: > 0 })
            return Array.Empty<Comic>();

        var comics = searchResults
            .Select(result =>
            {
                return new Comic(
                    Id: result.Hid,
                    ComicUrl: null,
                    Slug: null,
                    Title: result.Title,
                    Author: null,
                    Description: null,
                    Tags: [],
                    Year: null,
                    CoverImageUrl: result.CoverUrl.Medium,
                    Langs: []
                );
            })
            .ToList();

        return comics;
    }

    public async Task<IReadOnlyList<Comic>> GetTrendingAsync(
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters,
        int page = 1,
        CancellationToken ct = default
    )
    {
        var trendingFilters = new Dictionary<string, IReadOnlyList<string>>(filters)
        {
            ["order[trending]"] = ["desc"],
        };

        return await SearchAsync(string.Empty, trendingFilters, page, ct);
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

    private async Task<T?> GetFromApiAsync<T>(
        string endpoint,
        Dictionary<string, string[]>? queryParams = null,
        CancellationToken ct = default
    )
    {
        var url = $"{BaseUrl}{endpoint}";
        var allParams = queryParams ?? new Dictionary<string, string[]>();

        var vrf = FireCrypto.ComputeVrf(url, allParams);
        allParams["vrf"] = [vrf];

        var fullUrl = $"{url}?{ToQueryString(allParams)}";

        using var response = await _httpClient!.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, fullUrl),
            ct
        );

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "MangaFire Rate Limit Exceeded. Try again later.",
                null,
                HttpStatusCode.TooManyRequests
            );

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    private static string ToQueryString(Dictionary<string, string[]> source) =>
        string.Join(
            "&",
            source
                .OrderBy(kvp => kvp.Key)
                .SelectMany(kvp =>
                    kvp.Value.Where(v => !string.IsNullOrEmpty(v))
                        .Select(v => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(v)}")
                )
        );
}
