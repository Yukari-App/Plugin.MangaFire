using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record FireChapter(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("number")] float Number,
    [property: JsonPropertyName("name")] string? Title,
    [property: JsonPropertyName("language")] string Lang,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("createdAt")] int CreatedAt,
    [property: JsonPropertyName("pages")] Page[] Pages
);
