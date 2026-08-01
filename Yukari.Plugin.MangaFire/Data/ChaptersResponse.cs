using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record ChaptersResponse(
    [property: JsonPropertyName("items")] FireChapter[] Chapters,
    [property: JsonPropertyName("meta")] ChaptersResponseMeta Meta
);
