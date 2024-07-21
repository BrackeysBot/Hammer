using DSharpPlus;
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

        string iconUrl = guild.GetIconUrl(ImageFormat.Png);
        embedBuilder.WithFooter(guild.Name, iconUrl: iconUrl);
        if (addThumbnail) embedBuilder.WithThumbnail(iconUrl);
        return embedBuilder;
    }


    /// <summary>
    ///     Adds a field of any value type to the embed.
    /// </summary>
    /// <param name="builder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="name">The name of the embed field.</param>
    /// <param name="value">The value of the embed field.</param>
    /// <param name="inline"><see langword="true" /> to display this field inline; otherwise, <see langword="false" />.</param>
    /// <typeparam name="T">The type of <paramref name="value" />.</typeparam>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />; that is, <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static DiscordEmbedBuilder AddField<T>(
        this DiscordEmbedBuilder builder,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddField(name, value?.ToString(), inline);
    }

    /// <summary>
    ///     Conditionally adds a field to the embed.
    /// </summary>
    /// <param name="builder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="condition">The condition whose value is used to determine whether the field will be added.</param>
    /// <param name="name">The name of the embed field.</param>
    /// <param name="value">The value of the embed field.</param>
    /// <param name="inline"><see langword="true" /> to display this field inline; otherwise, <see langword="false" />.</param>
    /// <typeparam name="T">The type of <paramref name="value" />.</typeparam>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />; that is, <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        bool condition,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (condition)
        {
            builder.AddField(name, value?.ToString(), inline);
        }

        return builder;
    }

    /// <summary>
    ///     Conditionally adds a field to the embed.
    /// </summary>
    /// <param name="builder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="predicate">The predicate whose return value is used to determine whether the field will be added.</param>
    /// <param name="name">The name of the embed field.</param>
    /// <param name="value">The value of the embed field.</param>
    /// <param name="inline"><see langword="true" /> to display this field inline; otherwise, <see langword="false" />.</param>
    /// <typeparam name="T">The type of <paramref name="value" />.</typeparam>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />; that is, <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="builder" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="predicate" /> is <see langword="null" />.</para>
    /// </exception>
    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        Func<bool> predicate,
        string name,
        T? value,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (predicate.Invoke())
        {
            builder.AddField(name, value?.ToString(), inline);
        }

        return builder;
    }

    /// <summary>
    ///     Conditionally adds a field to the embed.
    /// </summary>
    /// <param name="builder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="predicate">The predicate whose return value is used to determine whether the field will be added.</param>
    /// <param name="name">The name of the embed field.</param>
    /// <param name="valueFactory">The delegate whose return value will be used as the value of the embed field.</param>
    /// <param name="inline"><see langword="true" /> to display this field inline; otherwise, <see langword="false" />.</param>
    /// <typeparam name="T">The return type of <paramref name="valueFactory" />.</typeparam>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />; that is, <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="builder" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="predicate" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="valueFactory" /> is <see langword="null" />.</para>
    /// </exception>
    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        Func<bool> predicate,
        string name,
        Func<T?> valueFactory,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        if (predicate.Invoke())
        {
            builder.AddField(name, valueFactory.Invoke()?.ToString(), inline);
        }

        return builder;
    }

    /// <summary>
    ///     Conditionally adds a field to the embed.
    /// </summary>
    /// <param name="builder">The <see cref="DiscordEmbedBuilder" /> to modify.</param>
    /// <param name="condition">The condition whose value is used to determine whether the field will be added.</param>
    /// <param name="name">The name of the embed field.</param>
    /// <param name="valueFactory">The delegate whose return value will be used as the value of the embed field.</param>
    /// <param name="inline"><see langword="true" /> to display this field inline; otherwise, <see langword="false" />.</param>
    /// <typeparam name="T">The return type of <paramref name="valueFactory" />.</typeparam>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />; that is, <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="builder" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="valueFactory" /> is <see langword="null" />.</para>
    /// </exception>
    public static DiscordEmbedBuilder AddFieldIf<T>(
        this DiscordEmbedBuilder builder,
        bool condition,
        string name,
        Func<T?> valueFactory,
        bool inline = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (valueFactory is null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        if (condition)
        {
            builder.AddField(name, valueFactory.Invoke()?.ToString(), inline);
        }

        return builder;
    }

    /// <summary>
    ///     Sets the embed's author.
    /// </summary>
    /// <param name="builder">The embed builder to modify.</param>
    /// <param name="user">The author.</param>
    /// <returns>The current instance of <see cref="DiscordEmbedBuilder" />.</returns>
    public static DiscordEmbedBuilder WithAuthor(this DiscordEmbedBuilder builder, DiscordUser user)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return builder.WithAuthor(user.GetUsernameWithDiscriminator(), iconUrl: user.AvatarUrl);
    }
}
