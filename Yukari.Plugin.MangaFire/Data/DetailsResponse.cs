using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record DetailsResponse([property: JsonPropertyName("data")] FireComic Comic);
