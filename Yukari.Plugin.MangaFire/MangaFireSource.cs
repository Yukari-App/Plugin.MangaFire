using System.Globalization;
using System.Net;
using Yukari.Core.Models;
using Yukari.Core.Models.Settings;
using Yukari.Core.Sources;
using Yukari.Core.Sources.Browser;
using Yukari.Core.Sources.Http;
using Yukari.Plugin.MangaFire.Data;

namespace Yukari.Plugin.MangaFire;

[ComicSourceMetadata(
    "MangaFire",
    "1.2.0+core2.6.0",
    "https://github.com/Yukari-App/Plugin.MangaFire/releases",
    "https://mangafire.to/assets/mangafire/logo-sm.png",
    "Access a vast array of manga in different languages."
)]
public partial class MangaFireSource
    : IComicSource,
        IRequiresHttpClient,
        IRequiresBrowser,
        IConfigurableSource
{
    private const int DefaultPageSize = 24;
    private const string BaseUrl = "https://mangafire.to/api";

    private ISharedHttpClient? _httpClient;
    private IBrowserService? _browserService;
    private BrowserSession? _cachedSession;
    private bool _useWebViewForChallenges = true;

    public IReadOnlyList<Filter> Filters => _filters;
    public IReadOnlyDictionary<string, string> Languages => _languages;
    public IReadOnlyList<SourceSetting> Settings => _settings;

    public void SetHttpClient(ISharedHttpClient httpClient) => _httpClient = httpClient;

    public void SetBrowser(IBrowserService browserService) => _browserService = browserService;

    public void ApplySettings(ISourceSettings settings)
    {
        _useWebViewForChallenges = settings.GetBool("use_webview_challenge", defaultValue: true);
        if (!_useWebViewForChallenges)
            _cachedSession = null;
    }

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
            queryParams["keyword"] = [query.Trim()];

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

    public async Task<Comic?> GetDetailsAsync(string comicId, CancellationToken ct = default)
    {
        string detailsEndpoint = $"/titles/{comicId}";

        var detailsResult = (
            await GetFromApiAsync<DetailsResponse>(detailsEndpoint, ct: ct)
        )?.Comic;
        if (detailsResult is null)
            return null;

        return new Comic(
            Id: detailsResult.Hid,
            ComicUrl: $"https://mangafire.to/title/{detailsResult.Hid}-{detailsResult.Slug}",
            Title: detailsResult.Title,
            Author: detailsResult.Authors.FirstOrDefault()?.Name,
            Description: detailsResult.DescriptionHTML,
            Tags: detailsResult.Genres.Select(t => t.Name).ToArray(),
            Year: detailsResult.Year,
            CoverImageUrl: detailsResult.CoverUrl.Medium,
            Langs: detailsResult.Langs,
            Status: GetComicStatus(detailsResult.Status)
        );
    }

    public async Task<IReadOnlyList<Chapter>> GetAllChaptersAsync(
        string comicId,
        string language,
        CancellationToken ct = default
    )
    {
        const int limit = 100;

        List<FireChapter> chapterResults = new();
        int page = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var queryParams = new Dictionary<string, string[]>
            {
                ["limit"] = [limit.ToString()],
                ["page"] = [page.ToString()],
                ["sort"] = ["number"],
                ["order"] = ["asc"],
                ["language"] = [language],
            };

            string chaptersEndpoint = $"/titles/{comicId}/chapters";

            var chaptersResponse = await GetFromApiAsync<ChaptersResponse>(
                chaptersEndpoint,
                queryParams,
                ct
            );

            if (chaptersResponse == null)
                return Array.Empty<Chapter>();

            if (chaptersResponse?.Chapters is { Length: > 0 } data)
                chapterResults.AddRange(data);
            else
                break;

            if (chapterResults.Count >= chaptersResponse.Meta.Total)
                break;

            page++;
            await Task.Delay(100, ct);
        }

        return chapterResults
            .Select(result => new Chapter(
                Id: result.Id.ToString(),
                Title: result.Title,
                Number: result.Number.ToString("0.#", CultureInfo.InvariantCulture),
                Volume: null,
                Language: result.Lang,
                Groups: result.Type == "official" ? ["Official"] : [],
                LastUpdate: DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(result.CreatedAt).Date
                ),
                Pages: null
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<ChapterPage>> GetChapterPagesAsync(
        string comicId,
        string chapterId,
        CancellationToken ct = default
    )
    {
        string pagesEndpoint = $"/chapters/{chapterId}";

        var chapterResult = (
            await GetFromApiAsync<ChapterDetailsResponse>(pagesEndpoint, ct: ct)
        )?.Chapter;
        if (chapterResult is not { Pages.Length: > 0 })
            return Array.Empty<ChapterPage>();

        var pages = new List<ChapterPage>(chapterResult.Pages.Length);
        for (int i = 0; i < chapterResult.Pages.Length; i++)
        {
            pages.Add(new ChapterPage(Number: i + 1, ImageUrl: chapterResult.Pages[i].ImageUrl));
        }

        return pages;
    }

    public async Task<byte[]?> GetImageBytesAsync(string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return null;

        await EnsureBrowserSessionAsync(ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        AddRequestHeaders(request, isApiRequest: false);

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
