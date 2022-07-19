using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Resources;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordEmbed" /> and <see cref="DiscordEmbedBuilder" />.
/// </summary>
internal static class DiscordEmbedExtensions
{
    /// <summary>
    ///     Adds the mention string of a <see cref="DiscordUser" /> as a field.
    /// </summary>
    /// <param name="embedBuilder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="name">The field name.</param>
    /// <param name="user">The user whose mention to add.</param>
    /// <param name="inline"><see langword="true" /> to display the field inline; otherwise, <see langword="false" />.</param>
    /// <returns><paramref name="embedBuilder" />, to allow for method chaining.</returns>
    public static DiscordEmbedBuilder AddField(this DiscordEmbedBuilder embedBuilder, string name, DiscordUser? user,
        bool inline = false)
    {
        if (user is null) return embedBuilder.AddField(name, Formatter.Italic("<does not exist>"), inline);
        return embedBuilder.AddField(name, user.Mention, inline);
    }

    /// <summary>
    ///     Populates the thumbnail and footer of this embed builder with the guild's branding.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="guild">The guild whose branding to apply.</param>
    /// <param name="addThumbnail">
    ///     <see langword="true" /> to include the guild icon as a thumbnail; otherwise, <see langword="false" />.
    /// </param>
    /// <returns><paramref name="embedBuilder" />, to allow for method chaining.</returns>
    public static DiscordEmbedBuilder AddGuildInfo(this DiscordEmbedBuilder embedBuilder, DiscordGuild guild,
        bool addThumbnail = true)
    {
        embedBuilder.WithFooter(guild.Name, guild.IconUrl);

        if (addThumbnail) embedBuilder.WithThumbnail(guild.IconUrl);
        return embedBuilder;
    }

    /// <summary>
    ///     Adds a notice to the end of the embed description notifying that the user should DM ModMail to discuss something with
    ///     staff.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <returns><paramref name="embedBuilder" />, to allow for method chaining.</returns>
    public static DiscordEmbedBuilder AddModMailNotice(this DiscordEmbedBuilder embedBuilder)
    {
        embedBuilder.AddField("\u200B", EmbedMessages.DmModMail);
        return embedBuilder;
    }
}
