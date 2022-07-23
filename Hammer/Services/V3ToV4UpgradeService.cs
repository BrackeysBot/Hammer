// silent CS0612. obsolete symbols are necessary for migration
// silent CS0618. obsolete symbols are necessary for migration

#pragma warning disable CS0612
#pragma warning disable CS0618

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Hammer.Data.v3_compat;
using Infraction = Hammer.Data.Infraction;
using InfractionType = Hammer.Data.InfractionType;
using LegacyInfraction = Hammer.Data.v3_compat.Infraction;
using LegacyInfractionType = Hammer.Data.v3_compat.InfractionType;

namespace Hammer.Services;

/// <summary>
///     Represents a class which performs migration from a legacy infraction.
/// </summary>
internal class V3ToV4UpgradeService
{
    private readonly DiscordClient _discordClient;

    public V3ToV4UpgradeService(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Returns a number indicating how many of the <see cref="UserData" /> entries are considered invalid due to a
    ///     non-existent user.
    /// </summary>
    /// <param name="userDatas">The user data objects</param>
    /// <returns>The number of non-existent users.</returns>
    public async Task<int> GetInvalidUserCountAsync(IEnumerable<UserData> userDatas)
    {
        var count = 0;

        foreach (UserData userData in userDatas)
        {
            try
            {
                await _discordClient.GetUserAsync(userData.ID).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                count++;
                userData.Invalid = true;
            }
        }

        return count;
    }

    /// <summary>
    ///     Enumerates the temporary bans of the specified legacy <see cref="UserData" /> object, constructing database-managed
    ///     temporary bans.
    /// </summary>
    /// <param name="guild">The guild to which these temporary bans apply.</param>
    /// <param name="userData">The user data to migrate.</param>
    /// <param name="forceInvalid">A value indicating whether to include invalid entries in the migration.</param>
    /// <returns>An enumerable of <see cref="TemporaryBan" /> objects.</returns>
    public async IAsyncEnumerable<TemporaryBan> EnumerateMigratedTemporaryBansAsync(DiscordGuild guild, UserData userData,
        bool forceInvalid)
    {
        DiscordUser? user;
        try
        {
            user = await _discordClient.GetUserAsync(userData.ID, true);
        }
        catch (NotFoundException)
        {
            user = null;
            if (!forceInvalid)
                yield break;
        }

        foreach (TemporaryInfraction legacyInfraction in
                 userData.TemporaryInfractions.Where(i => i.Type == TemporaryInfractionType.TempBan))
        {
            if (user is not null || forceInvalid)
                yield return TemporaryBan.Create(userData.ID, guild.Id, legacyInfraction.Expire);
        }
    }

    /// <summary>
    ///     Enumerates the mutes of the specified legacy <see cref="UserData" /> object, constructing database-managed mutes.
    /// </summary>
    /// <param name="guild">The guild to which these mutes apply.</param>
    /// <param name="userData">The user data to migrate.</param>
    /// <param name="forceInvalid">A value indicating whether to include invalid entries in the migration.</param>
    /// <returns>An enumerable of <see cref="Mute" /> objects.</returns>
    public async IAsyncEnumerable<Mute> EnumerateMigratedMutesAsync(DiscordGuild guild, UserData userData,
        bool forceInvalid)
    {
        DiscordUser? user;
        try
        {
            user = await _discordClient.GetUserAsync(userData.ID, true);
        }
        catch (NotFoundException)
        {
            user = null;
            if (!forceInvalid)
                yield break;
        }

        foreach (LegacyInfraction _ in userData.Infractions.Where(i => i.Type == LegacyInfractionType.Mute))
        {
            if (user is not null || forceInvalid)
                yield return Mute.Create(userData.ID, guild.Id);
        }

        foreach (TemporaryInfraction legacyInfraction in
                 userData.TemporaryInfractions.Where(i => i.Type == TemporaryInfractionType.TempMute))
        {
            if (user is not null || forceInvalid)
                yield return Mute.Create(userData.ID, guild.Id, legacyInfraction.Expire);
        }
    }

    /// <summary>
    ///     Enumerates the infractions of the specified legacy <see cref="UserData" /> object, constructing database-managed
    ///     infractions.
    /// </summary>
    /// <param name="guild">The guild to which these infractions apply.</param>
    /// <param name="userData">The user data to migrate.</param>
    /// <param name="forceInvalid">A value indicating whether to include invalid entries in the migration.</param>
    /// <returns>An enumerable of <see cref="Infraction" /> objects.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="userData" /> contains an infraction whose <see cref="Data.v3_compat.InfractionType" /> is invalid.
    /// </exception>
    public async IAsyncEnumerable<Infraction> EnumerateMigratedInfractionsAsync(DiscordGuild guild, UserData userData,
        bool forceInvalid)
    {
        DiscordUser? user;
        try
        {
            user = await _discordClient.GetUserAsync(userData.ID, true);
        }
        catch (NotFoundException)
        {
            user = null;
            if (!forceInvalid)
                yield break;
        }

        foreach (LegacyInfraction legacyInfraction in userData.Infractions)
        {
            InfractionType type = legacyInfraction.Type switch
            {
                LegacyInfractionType.Warning => InfractionType.Warning,
                LegacyInfractionType.TemporaryMute => InfractionType.TemporaryMute,
                LegacyInfractionType.Mute => InfractionType.Mute,
                LegacyInfractionType.Kick => InfractionType.Kick,
                LegacyInfractionType.TemporaryBan => InfractionType.TemporaryBan,
                LegacyInfractionType.Ban => InfractionType.Ban,
                _ => throw new ArgumentOutOfRangeException(nameof(legacyInfraction),
                    $"Unexpected infraction type {legacyInfraction.Type}")
            };

            if (user is not null || forceInvalid)
            {
                var infraction = new Infraction
                {
                    Id = legacyInfraction.ID,
                    Type = type,
                    GuildId = guild.Id,
                    IssuedAt = legacyInfraction.Time,
                    Reason = legacyInfraction.Description,
                    StaffMemberId = legacyInfraction.Moderator,
                    UserId = userData.ID
                };

                yield return infraction;
            }
        }
    }
}
