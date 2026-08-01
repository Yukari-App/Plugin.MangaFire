using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record FireComic(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("hid")] string Hid,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("authors")] Prop[] Authors,
    [property: JsonPropertyName("synopsisHtml")] string DescriptionHTML,
    [property: JsonPropertyName("genres")] Prop[] Genres,
    [property: JsonPropertyName("year")] int? Year,
    [property: JsonPropertyName("poster")] Poster CoverUrl,
    [property: JsonPropertyName("status")] string Status
);
