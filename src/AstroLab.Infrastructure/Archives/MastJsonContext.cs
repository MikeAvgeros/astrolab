using System.Text.Json.Serialization;

namespace AstroLab.Infrastructure.Archives;

[JsonSerializable(typeof(MastMashupRequest))]
[JsonSerializable(typeof(MastMashupResponse))]
internal partial class MastJsonContext : JsonSerializerContext
{
}
