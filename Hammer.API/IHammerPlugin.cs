using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrackeysBot.API.Plugins;
using DSharpPlus.Entities;

namespace Hammer.API;

/// <summary>
///     Represents a Hammer plugin instance.
/// </summary>
public interface IHammerPlugin : IPlugin
{
    /// <summary>
    ///     Issues a ban against a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember);

    /// <summary>
    ///     Issues a ban against a user, with a specified reason.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason);

    /// <summary>
    ///     Issues a temporary ban against a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="duration">The duration of the ban.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="duration" /> refers to a negative duration.</exception>
    Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration);

    /// <summary>
    ///     Issues a temporary ban against a user with a specified reason.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <param name="duration">The duration of the ban.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentException">
    ///     <para><paramref name="duration" /> refers to a negative duration.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration);

    /// <summary>
    ///     Deletes a specified message, logging the deletion in the staff log and optionally notifying the author.
    /// </summary>
    /// <param name="message">The message to delete.</param>
    /// <param name="staffMember">The staff member responsible for the deletion.</param>
    /// <param name="notifyAuthor">
    ///     <see langword="true" /> to notify the author of the deletion; otherwise, <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="NotSupportedException">The message does not belong to a guild.</exception>
    /// <exception cref="ArgumentException">
    ///     The guild in which the message appears does not match the guild of <paramref name="staffMember" />.
    /// </exception>
    Task DeleteMessageAsync(DiscordMessage message, DiscordMember staffMember, bool notifyAuthor = true);

    /// <summary>
    ///     Returns an enumerable collection of the infractions in a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>An enumerable collection of the infractions in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    IEnumerable<IInfraction> EnumerateInfractions(DiscordGuild guild);

    /// <summary>
    ///     Returns an enumerable collection of the infractions held by a user in a specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to retrieve.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>
    ///     An enumerable collection of the infractions held by <paramref name="user" /> in <paramref name="guild" />.
    ///     <exception cref="ArgumentNullException">
    ///         <para><paramref name="user" /> is <see langword="null" />.</para>
    ///         or
    ///         <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     </exception>
    /// </returns>
    IEnumerable<IInfraction> EnumerateInfractions(DiscordUser user, DiscordGuild guild);

    /// <summary>
    ///     Returns the count of infractions in a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>The number of infractions in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    int GetInfractionCount(DiscordGuild guild);

    /// <summary>
    ///     Returns the count of infractions held by a user in a specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to retrieve.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>The number of infractions held by <paramref name="user" /> in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    int GetInfractionCount(DiscordUser user, DiscordGuild guild);

    /// <summary>
    ///     Returns the infractions in a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>A read-only view of the infractions in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    IReadOnlyList<IInfraction> GetInfractions(DiscordGuild guild);

    /// <summary>
    ///     Returns the infractions held by a user in a specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to retrieve.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>A read-only view of the infractions held by <paramref name="user" /> in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    IReadOnlyList<IInfraction> GetInfractions(DiscordUser user, DiscordGuild guild);

    /// <summary>
    ///     Returns a value indicating whether a user is currently muted in the specified guild.
    /// </summary>
    /// <param name="user">The user whose mute status to check.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns><see langword="true" /> if <paramref name="user" /> is muted; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    bool IsUserMuted(DiscordUser user, DiscordGuild guild);

    /// <summary>
    ///     Kicks a member from the guild.
    /// </summary>
    /// <param name="member">The member to kick.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentException">
    ///     <para><paramref name="member" /> and <paramref name="staffMember" /> are not in the same guild.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember);

    /// <summary>
    ///     Kicks a member from the guild.
    /// </summary>
    /// <param name="member">The member to kick.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentException">
    ///     <para><paramref name="member" /> and <paramref name="staffMember" /> are not in the same guild.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, string reason);

    /// <summary>
    ///     Issues a mute against a user.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember);

    /// <summary>
    ///     Issues a mute against a user, with a specified reason.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason);

    /// <summary>
    ///     Issues a temporary mute against a user.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="duration">The duration of the mute.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentException"><paramref name="duration" /> refers to a negative duration.</exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration);

    /// <summary>
    ///     Issues a temporary mute against a user with a specified reason.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <param name="duration">The duration of the mute.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentException"><paramref name="duration" /> refers to a negative duration.</exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration);

    /// <summary>
    ///     Issues a warning against a user, with a specified reason.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    Task<IInfraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason);
}
