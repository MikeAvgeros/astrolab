using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

[JsonSerializable(typeof(MastMashupRequest))]
[JsonSerializable(typeof(MastMashupResponse))]
[JsonSerializable(typeof(MastProductRequest))]
[JsonSerializable(typeof(MastProductResponse))]
[JsonSerializable(typeof(MastNameLookupRequest))]
[JsonSerializable(typeof(MastNameLookupResponse))]
internal partial class MastJsonContext : JsonSerializerContext
{
}
