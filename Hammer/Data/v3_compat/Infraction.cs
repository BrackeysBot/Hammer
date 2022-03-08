using System;
using System.Text.Json.Serialization;

namespace Hammer.Data.v3_compat;

[Obsolete("This type exists for migration purposes. Please use Hammer.Data.Infraction")]
internal struct Infraction
{
    [JsonPropertyName("id")]
    public int ID { get; set; }

    [JsonPropertyName("moderator")]
    public ulong Moderator { get; set; }

    [JsonPropertyName("type")]
    public InfractionType Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("additionalInfo")]
    public string AdditionalInfo { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }
}
