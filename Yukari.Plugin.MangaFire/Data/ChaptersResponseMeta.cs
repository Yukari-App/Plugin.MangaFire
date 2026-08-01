using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record ChaptersResponseMeta([property: JsonPropertyName("total")] int Total);
