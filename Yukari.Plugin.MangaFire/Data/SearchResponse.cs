using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record SearchResponse([property: JsonPropertyName("items")] FireComic[] Comics);
