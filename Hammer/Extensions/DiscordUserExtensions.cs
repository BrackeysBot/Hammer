using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordUser" />.
/// </summary>
internal static class DiscordUserExtensions
{
    /// <summary>
    ///     Returns the current <see cref="DiscordUser" /> as a member of the specified guild.
    /// </summary>
    /// <param name="user">The user to transform.</param>
    /// <param name="guild">The guild whose member list to search.</param>
    /// <returns>
    ///     A <see cref="DiscordMember" /> whose <see cref="DiscordMember.Guild" /> is equal to <paramref name="guild" />, or
    ///     <see langword="null" /> if this user is not in the specified <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public static async Task<DiscordMember?> GetAsMemberOfAsync(this DiscordUser user, DiscordGuild guild)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (guild is null)
        {
            throw new ArgumentNullException(nameof(guild));
        }

        if (user is DiscordMember member && member.Guild == guild)
        {
            return member;
        }

        if (guild.Members.TryGetValue(user.Id, out member!))
        {
            return member;
        }

        try
        {
            return await guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets the permission level for this member.
    /// </summary>
    /// <param name="member">The member whose permission level to retrieve.</param>
    /// <param name="guildConfiguration">The guild configuration.</param>
    /// <returns>The permission level.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guildConfiguration" /> is <see langword="null" />.</para>
    /// </exception>
    public static PermissionLevel GetPermissionLevel(this DiscordMember member, GuildConfiguration guildConfiguration)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(guildConfiguration);

        if ((member.Permissions & DSharpPlus.Permissions.Administrator) != 0)
            return PermissionLevel.Administrator;

        RoleConfiguration roleConfiguration = guildConfiguration.Roles;
        List<ulong> roles = member.Roles.Select(r => r.Id).ToList();

        if (roles.Contains(roleConfiguration.AdministratorRoleId)) return PermissionLevel.Administrator;
        if (roles.Contains(roleConfiguration.ModeratorRoleId)) return PermissionLevel.Moderator;
        if (roles.Contains(roleConfiguration.GuruRoleId)) return PermissionLevel.Guru;

        return PermissionLevel.Default;
    }

    /// <summary>
    ///     Returns the user's username with the discriminator, in the format <c>username#discriminator</c>.
    /// </summary>
    /// <param name="user">The user whose username and discriminator to retrieve.</param>
    /// <returns>A string in the format <c>username#discriminator</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="user" /> is <see langword="null" />.</exception>
    public static string GetUsernameWithDiscriminator(this DiscordUser user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (user.Discriminator == "0")
        {
            // user has a new username. see: https://discord.com/blog/usernames
            return user.Username;
        }

        return $"{user.Username}#{user.Discriminator}";
    }

    /// <summary>
    ///     Returns a value indicating whether the member is a higher permission level than another member.
    /// </summary>
    /// <param name="member">The member whose permission level to check.</param>
    /// <param name="other">The member whose permission level to compare with.</param>
    /// <param name="guildConfiguration">The guild configuration.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is a higher permission level than <paramref name="other"/>;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="other" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guildConfiguration" /> is <see langword="null" />.</para>
    /// </exception>
    public static bool IsHigherLevelThan(this DiscordMember member, DiscordMember other, GuildConfiguration guildConfiguration)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(guildConfiguration);

        if (member.IsStaffMember(guildConfiguration) && !other.IsStaffMember(guildConfiguration))
            return true;

        return GetPermissionLevel(member, guildConfiguration) > GetPermissionLevel(other, guildConfiguration);
    }

    /// <summary>
    ///     Returns a value indicating whether the member is a higher or equal permission level than another member.
    /// </summary>
    /// <param name="member">The member whose permission level to check.</param>
    /// <param name="other">The member whose permission level to compare with.</param>
    /// <param name="guildConfiguration">The guild configuration.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is a higher or equal permission level than
    ///     <paramref name="other"/>; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="other" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guildConfiguration" /> is <see langword="null" />.</para>
    /// </exception>
    public static bool IsHigherOrSameLevel(this DiscordMember member, DiscordMember other, GuildConfiguration guildConfiguration)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(guildConfiguration);

        if (GetPermissionLevel(member, guildConfiguration) >= GetPermissionLevel(other, guildConfiguration))
            return true;

        return member.Roles.Min(r => r.Position) <= other.Roles.Min(r => r.Position);
    }

    /// <summary>
    ///     Returns a value indicating whether the member is a staff member.
    /// </summary>
    /// <param name="member">The member whose staff status to retrieve.</param>
    /// <param name="guildConfiguration">The guild configuration.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is a staff member; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guildConfiguration" /> is <see langword="null" />.</para>
    /// </exception>
    public static bool IsStaffMember(this DiscordMember member, GuildConfiguration guildConfiguration)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(guildConfiguration);

        return GetPermissionLevel(member, guildConfiguration) >= PermissionLevel.Moderator;
    }
}
