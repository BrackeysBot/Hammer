using DSharpPlus.Entities;
using Hammer.Resources;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordEmbedBuilder" />.
/// </summary>
internal static class DiscordEmbedBuilderExtensions
{
    /// <summary>
    ///     Adds a notice to the end of the embed description notifying that the user should DM ModMail to discuss something with
    ///     staff.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <returns><paramref name="embedBuilder" />, to allow for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="embedBuilder" /> is <see langword="null" />.</exception>
    public static DiscordEmbedBuilder AddModMailNotice(this DiscordEmbedBuilder embedBuilder)
    {
        ArgumentNullException.ThrowIfNull(embedBuilder);

        embedBuilder.AddField("\u200B", EmbedMessages.DmModMail);
        return embedBuilder;
    }

    /// <summary>
    ///     Adds a notice to the end of the embed description notifying that the user should DM ModMail to discuss something with
    ///     staff.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="guild">The guild whose branding to add.</param>
    /// <param name="addThumbnail">
    ///     <see langword="true" /> to show the guild's icon as the embed thumbnail; otherwise, <see langword="false" />.
    /// </param>
    /// <returns><paramref name="embedBuilder" />, to allow for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="embedBuilder" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public static DiscordEmbedBuilder WithGuildInfo(
        this DiscordEmbedBuilder embedBuilder,
        DiscordGuild guild,
        bool addThumbnail = true
    )
    {
        ArgumentNullException.ThrowIfNull(embedBuilder);
        ArgumentNullException.ThrowIfNull(guild);

        embedBuilder.WithFooter(guild.Name, iconUrl: guild.IconUrl);
        if (addThumbnail) embedBuilder.WithThumbnail(guild.IconUrl);
        return embedBuilder;
    }
}
