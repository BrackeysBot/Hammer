using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.API;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles sending direct messages to members for a variety of purposes.
/// </summary>
internal sealed class MailmanService
{
    private readonly HammerPlugin _hammerPlugin;
    private readonly DiscordClient _discordClient;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailmanService" /> class.
    /// </summary>
    public MailmanService(HammerPlugin hammerPlugin, DiscordClient discordClient, RuleService ruleService)
    {
        _hammerPlugin = hammerPlugin;
        _discordClient = discordClient;
        _ruleService = ruleService;
    }

    /// <summary>
    ///     Sends an infraction notice to the applicable member, if possible.
    /// </summary>
    /// <param name="infraction">The infraction to notify.</param>
    /// <returns>The message which was sent to the member, or <see langword="null" /> if the message could not be sent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> SendInfractionAsync(IInfraction infraction)
    {
        if (infraction is null) throw new ArgumentNullException(nameof(infraction));

        DiscordMember member;
        try
        {
            DiscordGuild guild = await _discordClient.GetGuildAsync(infraction.GuildId).ConfigureAwait(false);
            member = await guild.GetMemberAsync(infraction.UserId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            // bots can only DM users who are in the guild
            return null;
        }

        try
        {
            DiscordEmbed? embed = await CreatePrivateInfractionEmbedAsync(infraction).ConfigureAwait(false);
            if (embed is not null)
                return await member.SendMessageAsync(embed).ConfigureAwait(false);

            // user does not exist, or guild is invalid
            return null;
        }
        catch (UnauthorizedException)
        {
            // bot is blocked or DMs disabled
            return null;
        }
    }

    private async Task<DiscordEmbed?> CreatePrivateInfractionEmbedAsync(IInfraction infraction)
    {
        if (infraction.Type == InfractionType.Gag)
            throw new ArgumentException(ExceptionMessages.NoEmbedForGag, nameof(infraction));

        DiscordUser user;
        DiscordGuild guild;
        try
        {
            guild = await _discordClient.GetGuildAsync(infraction.GuildId).ConfigureAwait(false);
            user = await guild.GetMemberAsync(infraction.UserId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            // bots can only DM users who are in the guild, and if the guild is valid
            return null;
        }

        int infractionCount = _hammerPlugin.GetInfractionCount(infraction.UserId, infraction.GuildId);

        string? description = infraction.Type switch
        {
            InfractionType.Warning => EmbedMessages.WarningDescription,
            InfractionType.TemporaryMute => EmbedMessages.TemporaryMuteDescription,
            InfractionType.Mute => EmbedMessages.MuteDescription,
            InfractionType.Kick => EmbedMessages.KickDescription,
            InfractionType.Ban => EmbedMessages.BanDescription,
            InfractionType.TemporaryBan => EmbedMessages.TemporaryBanDescription,
            _ => null
        };

        string reason = infraction.Reason.WithWhiteSpaceAlternative(Formatter.Italic("No reason given."));
        Rule? rule = null;

        if (infraction.RuleId.HasValue)
            rule = _ruleService.GetRuleById(guild, infraction.RuleId.Value);

        return new DiscordEmbedBuilder()
            .WithColor(0xFF0000)
            .WithTitle(infraction.Type.Humanize())
            .WithDescription(string.IsNullOrWhiteSpace(description) ? null : description.FormatSmart(new {user, guild}))
            .WithThumbnail(guild.IconUrl)
            .WithFooter(guild.Name, guild.IconUrl)
            .AddFieldIf(rule is not null, EmbedFieldNames.RuleBroken, () => $"{rule!.Id} - {rule.Brief ?? rule.Content}", true)
            .AddField(EmbedFieldNames.TotalInfractions, infractionCount, true)
            .AddField(EmbedFieldNames.Reason, reason);
    }
}
