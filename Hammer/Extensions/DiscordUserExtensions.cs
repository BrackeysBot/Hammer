using DisCatSharp;
using DisCatSharp.Entities;
using Hammer.Configuration;
using PermissionLevel = Hammer.Data.PermissionLevel;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordUser" /> and <see cref="DiscordMember" />.
/// </summary>
internal static class DiscordUserExtensions
{
    /// <summary>
    ///     Gets the permission level for this member.
    /// </summary>
    /// <param name="member">The member whose permission level to retrieve.</param>
    /// <param name="roleConfiguration">The role configuration.</param>
    /// <returns>The member's permission level.</returns>
    public static PermissionLevel GetPermissionLevel(this DiscordMember member, RoleConfiguration roleConfiguration)
    {
        if ((member.Permissions & Permissions.Administrator) != 0) return PermissionLevel.Administrator;

        List<ulong> roles = member.Roles.Select(r => r.Id).ToList();
        if (roles.Contains(roleConfiguration.AdministratorRoleId)) return PermissionLevel.Administrator;
        if (roles.Contains(roleConfiguration.ModeratorRoleId)) return PermissionLevel.Moderator;
        if (roles.Contains(roleConfiguration.GuruRoleId)) return PermissionLevel.Guru;
        return PermissionLevel.Default;
    }

    /// <summary>
    ///     Determines if a member is a higher permission level than another member.
    /// </summary>
    /// <param name="member">The member whose permission level to check.</param>
    /// <param name="other">The member whose permission level to compare against.</param>
    /// <param name="roleConfiguration">The role configuration.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is considered a higher level than <paramref name="other" />;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsHigherLevelThan(this DiscordMember member, DiscordMember other, RoleConfiguration roleConfiguration)
    {
        return member.GetPermissionLevel(roleConfiguration) > other.GetPermissionLevel(roleConfiguration);
    }

    /// <summary>
    ///     Determines if the member is considered a staff member.
    /// </summary>
    /// <param name="member">The member whose permission level to check.</param>
    /// <param name="roleConfiguration">The role configuration.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is considered a staff member; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool IsStaffMember(this DiscordMember member, RoleConfiguration roleConfiguration)
    {
        return (int) member.GetPermissionLevel(roleConfiguration) >= (int) PermissionLevel.Moderator;
    }
}
