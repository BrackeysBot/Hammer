using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.ValueConverters;

/// <summary>
///     Converts a <see cref="DiscordGuild" /> to a <see cref="ulong" /> and vice versa.
/// </summary>
internal sealed class DiscordGuildValueConverter : ValueConverter<DiscordGuild, ulong>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DiscordGuildValueConverter" /> class.
    /// </summary>
    public DiscordGuildValueConverter(DiscordClient discordClient, ConverterMappingHints? mappingHints = null)
        : base(v => ToProvider(v), v => FromProvider(discordClient, v), mappingHints)
    {
    }

    private static ulong ToProvider(DiscordGuild guild)
    {
        return guild.Id;
    }

    private static DiscordGuild FromProvider(DiscordClient discordClient, ulong id)
    {
        try
        {
            return discordClient.GetGuildAsync(id).GetAwaiter().GetResult();
        }
        catch
        {
            return null!;
        }
    }
}
