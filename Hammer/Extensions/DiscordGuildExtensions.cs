﻿using DSharpPlus.Entities;
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
}
