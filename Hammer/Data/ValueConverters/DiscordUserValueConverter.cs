using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.ValueConverters;

/// <summary>
///     Converts a <see cref="DiscordUser" /> to a <see cref="ulong" /> and vice versa.
/// </summary>
internal sealed class DiscordUserValueConverter : ValueConverter<DiscordUser, ulong>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DiscordUserValueConverter" /> class.
    /// </summary>
    public DiscordUserValueConverter(DiscordClient discordClient, ConverterMappingHints? mappingHints = null)
        : base(v => ToProvider(v), v => FromProvider(discordClient, v), mappingHints)
    {
    }

    private static ulong ToProvider(DiscordUser user)
    {
        return user.Id;
    }

    private static DiscordUser FromProvider(DiscordClient discordClient, ulong id)
    {
        try
        {
            return discordClient.GetUserAsync(id).GetAwaiter().GetResult();
        }
        catch
        {
            return null!;
        }
    }
}
