using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record ChapterDetailsResponse([property: JsonPropertyName("data")] FireChapter Chapter);
