using System;

namespace Hammer.Data.v3_compat;

[Obsolete("This type exists for migration purposes. Please use Hammer.Data.InfractionType")]
internal enum InfractionType
{
    Warning,
    Ban,
    TemporaryBan,
    Mute,
    TemporaryMute,
    Kick
}
