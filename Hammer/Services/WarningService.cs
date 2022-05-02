using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.API;
using Hammer.Data;
using Hammer.Data.Infractions;
using Hammer.Extensions;
using Hammer.Resources;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages member warnings.
/// </summary>
internal sealed class WarningService
{
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discord;
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarningService" /> class.
    /// </summary>
    public WarningService(ICorePlugin corePlugin, DiscordClient discord, InfractionService infractionService)
    {
        _corePlugin = corePlugin;
        _discord = discord;
        _infractionService = infractionService;
    }

    /// <summary>
    ///     Warns a user with the specified reason.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="issuer">The staff member who issued the warning.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.
    /// </exception>
    public async Task<Infraction> WarnAsync(DiscordUser user, DiscordMember issuer, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason));

        DiscordGuild guild = await issuer.Guild.NormalizeClientAsync(_discord).ConfigureAwait(false);

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace()
        };

        Infraction infraction = await _infractionService.CreateInfractionAsync(InfractionType.Warning, user, issuer, options)
            .ConfigureAwait(false);
        int infractionCount = _infractionService.GetInfractionCount(user, guild);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithAuthor(user);
        embed.WithTitle("User warned");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, issuer.Mention, true);
        embed.AddFieldIf(infractionCount > 0, EmbedFieldNames.TotalUserInfractions, infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), EmbedFieldNames.Reason, options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");

        await _corePlugin.LogAsync(guild, embed).ConfigureAwait(false);
        return infraction;
    }
}
