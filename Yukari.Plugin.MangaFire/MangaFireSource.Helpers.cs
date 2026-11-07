using System.Net;
using System.Net.Http.Json;
using Yukari.Core.Models;
using Yukari.Core.Models.Settings;
using Yukari.Core.Sources.Browser;
using Yukari.Plugin.MangaFire.Helpers;

namespace Yukari.Plugin.MangaFire;

public partial class MangaFireSource
{
    private static readonly IReadOnlyDictionary<string, string> _languages = new Dictionary<
        string,
        string
    >
    {
        { "en", "English" },
        { "fr", "Français" },
        { "es", "Español" },
        { "es-la", "Español LA" },
        { "pt-br", "Português" },
        { "pt", "Português (EU)" },
        { "ja", "日本語" },
    };

    private static readonly IReadOnlyList<Filter> _filters =
    [
        new Filter(
            Key: "content_rating[]",
            DisplayName: "Content Rating",
            Options:
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
            Options:
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
            Options:
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
            Options:
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
            Options: _languages
                .Select(kvp => new FilterOption(kvp.Key, kvp.Value, false))
                .ToArray(),
            AllowMultiple: true
        ),
        new Filter(
            Key: "genres_in[]",
            DisplayName: "Genres",
            Options:
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
    ];

    private static readonly IReadOnlyList<SourceSetting> _settings =
    [
        new ToggleSetting
        {
            Key = "use_webview_challenge",
            Header = "Use WebView for Challenges",
            Description =
                "Uses an embedded browser to resolve Cloudflare challenges when required.",
            DefaultValue = true,
        },
    ];

    private async Task<T?> GetFromApiAsync<T>(
        string endpoint,
        Dictionary<string, string[]>? queryParams = null,
        CancellationToken ct = default
    )
    {
        var url = $"{BaseUrl}{endpoint}";
        var allParams = queryParams ?? new Dictionary<string, string[]>();

        await EnsureBrowserSessionAsync(ct);

        var vrf = FireCrypto.ComputeVrf(url, allParams);
        allParams["vrf"] = [vrf];

        var fullUrl = $"{url}?{ToQueryString(allParams)}";

        HttpResponseMessage? response = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            AddRequestHeaders(request);
            response = await _httpClient!.SendAsync(request, ct);

            if (
                response.StatusCode == HttpStatusCode.Forbidden
                && _cachedSession != null
                && _useWebViewForChallenges
            )
            {
                response.Dispose();

                _cachedSession = null;
                await EnsureBrowserSessionAsync(ct);

                using var retryRequest = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                AddRequestHeaders(retryRequest);
                response = await _httpClient!.SendAsync(retryRequest, ct);
            }

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
        finally
        {
            response?.Dispose();
        }
    }

    private void AddRequestHeaders(HttpRequestMessage request, bool isApiRequest = true)
    {
        request.Headers.Add("Referer", "https://mangafire.to/");

        var headers = GetSessionHeaders();
        var userAgent =
            headers?.UserAgent
            ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:153.0) Gecko/20100101 Firefox/153.0";
        request.Headers.Add("User-Agent", userAgent);

        if (headers != null && !string.IsNullOrEmpty(headers.Value.CookieHeader))
            request.Headers.Add("Cookie", headers.Value.CookieHeader);

        if (isApiRequest)
        {
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        }
    }

    private (string CookieHeader, string UserAgent)? GetSessionHeaders()
    {
        if (_cachedSession == null)
            return null;
        var cookieString = string.Join(
            "; ",
            _cachedSession.Cookies.Select(c => $"{c.Key}={c.Value}")
        );
        return (cookieString, _cachedSession.UserAgent);
    }

    private async Task<BrowserSession?> EnsureBrowserSessionAsync(CancellationToken ct)
    {
        if (!_useWebViewForChallenges)
            return null;

        if (_cachedSession != null)
            return _cachedSession;
        if (_browserService == null)
            return null;

        var session = await _browserService.ResolveChallengeAsync(
            "https://mangafire.to/",
            TimeSpan.FromSeconds(30),
            """
            (function() {
                var isChallenge = document.querySelector('.ray-id') ||
                    document.querySelector('script[src*="challenges.cloudflare.com"]');
                return !isChallenge;
            })();
            """,
            TimeSpan.FromSeconds(5),
            ct
        );

        if (session != null)
            _cachedSession = session;
        return _cachedSession;
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
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .SelectMany(kvp =>
                    kvp.Value.Where(v => !string.IsNullOrEmpty(v))
                        .Select(v => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(v)}")
                )
        );
}
