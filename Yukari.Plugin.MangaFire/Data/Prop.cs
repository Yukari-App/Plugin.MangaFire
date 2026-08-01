using System.Text.Json.Serialization;

namespace Yukari.Plugin.MangaFire.Data;

internal record Prop([property: JsonPropertyName("title")] string Name);
