using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordUser" />.
/// </summary>
internal static class DiscordUserExtensions
{
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
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (guildConfiguration is null) throw new ArgumentNullException(nameof(guildConfiguration));

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
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (guildConfiguration is null) throw new ArgumentNullException(nameof(guildConfiguration));

        return GetPermissionLevel(member, guildConfiguration) > GetPermissionLevel(other, guildConfiguration);
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
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (guildConfiguration is null) throw new ArgumentNullException(nameof(guildConfiguration));

        return GetPermissionLevel(member, guildConfiguration) >= PermissionLevel.Moderator;
    }
}
