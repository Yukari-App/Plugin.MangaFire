using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record Poster([property: JsonPropertyName("medium")] string Medium);
