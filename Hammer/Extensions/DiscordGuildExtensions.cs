using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Configuration;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordGuild" />.
/// </summary>
internal static class DiscordGuildExtensions
{
    /// <summary>
    ///     Constructs an embed by populating the footer and thumbnail with the guild's branding.
    /// </summary>
    /// <param name="guild">The guild whose branding to apply.</param>
    /// <param name="guildConfiguration">The configuration from which to build.</param>
    /// <param name="addThumbnail">
    ///     <see langword="true" /> to include the guild icon as a thumbnail; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>A new <see cref="DiscordEmbedBuilder" /> with the footer and thumbnail assigned the guild's branding.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guildConfiguration" /> is <see langword="null" />.</para>
    /// </exception>
    public static DiscordEmbedBuilder CreateDefaultEmbed(
        this DiscordGuild guild,
        GuildConfiguration guildConfiguration,
        bool addThumbnail = true
    )
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(guildConfiguration);

        return new DiscordEmbedBuilder().WithColor(guildConfiguration.PrimaryColor).WithGuildInfo(guild, addThumbnail);
    }

    /// <summary>
    ///     Gets a guild member by their ID. If the member is not found, <see langword="null" /> is returned instead of
    ///     <see cref="NotFoundException" /> being thrown.
    /// </summary>
    /// <param name="guild">The guild whose member list to search.</param>
    /// <param name="userId">The ID of the member to retrieve.</param>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public static async Task<DiscordMember?> GetMemberOrNullAsync(this DiscordGuild guild, ulong userId)
    {
        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        try
        {
            // we should never use exceptions for flow control but this is D#+ we're talking about.
            // NotFoundException isn't even documented, and yet it gets thrown when a member doesn't exist.
            // so this method should hopefully clearly express that - and at least using exceptions for flow control *here*,
            // removes the need to do the same in consumer code.
            // god I hate this.
            return await guild.GetMemberAsync(userId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}
