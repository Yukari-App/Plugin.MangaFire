using Yukari.Core.Models;
using Yukari.Core.Sources;

namespace Yukari.Plugin.MangaFire;

[ComicSourceMetadata(
    "MangaFire",
    "0.1.0-Alpha+core2.2.0",
    "https://mangafire.to/assets/sites/mangafire/favicon.png?v3",
    "Access a vast array of manga in different languages."
)]
public class MangaFireSource : IComicSource
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
            { "jp", "日本語" },
        };

    private const string BaseUrl = "https://mangafire.to";

    private static readonly HttpClient _httpClient = new HttpClient();

    static MangaFireSource()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Yukari.Plugin.MangaFire/0.1.0");
    }

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

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
