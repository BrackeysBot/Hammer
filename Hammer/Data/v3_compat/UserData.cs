using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hammer.Data.v3_compat;

[Obsolete("This type exists for migration purposes. Please use Hammer.Data.Infraction")]
internal class UserData
{
    [JsonPropertyName("id")]
    public ulong ID { get; set; }

    [JsonPropertyName("temporaryInfractions")]
    public List<TemporaryInfraction> TemporaryInfractions { get; set; } = new();

    [JsonPropertyName("infractions")]
    public List<Infraction> Infractions { get; set; } = new();

    [JsonPropertyName("muted")]
    public bool Muted { get; set; }
}
