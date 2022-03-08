using System;
using System.Text.Json.Serialization;

namespace Hammer.Data.v3_compat;

[Obsolete("This type exists for migration purposes. Please use Hammer.Data.Infraction")]
internal struct TemporaryInfraction
{
    [JsonPropertyName("type")]
    public TemporaryInfractionType Type { get; set; }

    [JsonPropertyName("expire")]
    public DateTime Expire { get; set; }
}
