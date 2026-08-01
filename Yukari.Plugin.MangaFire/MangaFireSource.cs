using System.Globalization;
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

    public IReadOnlyList<Filter> Filters =>
        _filters ??= new List<Filter>
        {
            new Filter(
                Key: "content_rating[]",
                DisplayName: "Content Rating",
                [
                    new FilterOption("safe", "Safe", true),
                    new FilterOption("suggestive", "Suggestive", true),
                    new FilterOption("erotica", "Erotica", false),
                    new FilterOption("pornographic", "Pornographic", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "types[]",
                DisplayName: "Types",
                [
                    new FilterOption("manga", "Manga", false),
                    new FilterOption("manhwa", "Manhwa", false),
                    new FilterOption("manhua", "Manhua", false),
                    new FilterOption("other", "Other", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "demographics[]",
                DisplayName: "Demographics",
                [
                    new FilterOption("josei", "Josei", false),
                    new FilterOption("seinen", "Seinen", false),
                    new FilterOption("shoujo", "Shoujo", false),
                    new FilterOption("shounen", "Shounen", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "statuses[]",
                DisplayName: "Statuses",
                [
                    new FilterOption("releasing", "Ongoing", false),
                    new FilterOption("finished", "Completed", false),
                    new FilterOption("on_hiatus", "Hiatus", false),
                    new FilterOption("discontinued", "Cancelled", false),
                ],
                AllowMultiple: true
            ),
            new Filter(
                Key: "languages[]",
                DisplayName: "Language",
                Languages.Select(kvp => new FilterOption(kvp.Key, kvp.Value, false)).ToArray(),
                AllowMultiple: true
            ),
            new Filter(
                Key: "genres_in[]",
                DisplayName: "Genres",
                [
                    new FilterOption("1", "Action", false),
                    new FilterOption("268929", "Adult", false),
                    new FilterOption("78", "Adventure", false),
                    new FilterOption("3", "Avant Garde", false),
                    new FilterOption("4", "Boys Love", false),
                    new FilterOption("5", "Comedy", false),
                    new FilterOption("268921", "Crime", false),
                    new FilterOption("77", "Demons", false),
                    new FilterOption("6", "Drama", false),
                    new FilterOption("7", "Ecchi", false),
                    new FilterOption("79", "Fantasy", false),
                    new FilterOption("9", "Girls Love", false),
                    new FilterOption("10", "Gourmet", false),
                    new FilterOption("11", "Harem", false),
                    new FilterOption("268930", "Hentai", false),
                    new FilterOption("268922", "Historical", false),
                    new FilterOption("530", "Horror", false),
                    new FilterOption("13", "Isekai", false),
                    new FilterOption("531", "Iyashikei", false),
                    new FilterOption("15", "Josei", false),
                    new FilterOption("532", "Kids", false),
                    new FilterOption("539", "Magic", false),
                    new FilterOption("268923", "Magical Girls", false),
                    new FilterOption("533", "Mahou Shoujo", false),
                    new FilterOption("534", "Martial Arts", false),
                    new FilterOption("268931", "Mature", false),
                    new FilterOption("19", "Mecha", false),
                    new FilterOption("268924", "Medical", false),
                    new FilterOption("535", "Military", false),
                    new FilterOption("21", "Music", false),
                    new FilterOption("22", "Mystery", false),
                    new FilterOption("23", "Parody", false),
                    new FilterOption("268925", "Philosophical", false),
                    new FilterOption("536", "Psychological", false),
                    new FilterOption("25", "Reverse Harem", false),
                    new FilterOption("26", "Romance", false),
                    new FilterOption("73", "School", false),
                    new FilterOption("28", "Sci-Fi", false),
                    new FilterOption("537", "Seinen", false),
                    new FilterOption("30", "Shoujo", false),
                    new FilterOption("31", "Shounen", false),
                    new FilterOption("538", "Slice of Life", false),
                    new FilterOption("268932", "Smut", false),
                    new FilterOption("33", "Space", false),
                    new FilterOption("34", "Sports", false),
                    new FilterOption("75", "Super Power", false),
                    new FilterOption("268926", "Superhero", false),
                    new FilterOption("76", "Supernatural", false),
                    new FilterOption("37", "Suspense", false),
                    new FilterOption("38", "Thriller", false),
                    new FilterOption("268927", "Tragedy", false),
                    new FilterOption("39", "Vampire", false),
                    new FilterOption("268928", "Wuxia", false),
                ],
                AllowMultiple: true
            ),
        };

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
            Slug: null,
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

    private static ComicStatus GetComicStatus(string? status) =>
        status switch
        {
            "finished" => ComicStatus.Completed,
            "releasing" => ComicStatus.Ongoing,
            "on_hiatus" => ComicStatus.Hiatus,
            "discontinued" => ComicStatus.Cancelled,
            _ => ComicStatus.Unknown,
        };

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
