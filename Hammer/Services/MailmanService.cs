using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using SmartFormat;
using X10D.DSharpPlus;
using X10D.Text;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles sending direct messages to members for a variety of purposes.
/// </summary>
internal sealed class MailmanService
{
    private readonly DiscordClient _discordClient;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailmanService" /> class.
    /// </summary>
    public MailmanService(DiscordClient discordClient, RuleService ruleService)
    {
        _discordClient = discordClient;
        _ruleService = ruleService;
    }

    /// <summary>
    ///     Sends an infraction notice to the applicable member, if possible.
    /// </summary>
    /// <param name="infraction">The infraction to notify.</param>
    /// <param name="infractionCount">The infraction count to display on the embed.</param>
    /// <returns>The message which was sent to the member, or <see langword="null" /> if the message could not be sent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> SendInfractionAsync(Infraction infraction, int infractionCount)
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
            DiscordEmbed? embed = await CreatePrivateInfractionEmbedAsync(infraction, infractionCount).ConfigureAwait(false);
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

    private async Task<DiscordEmbed?> CreatePrivateInfractionEmbedAsync(Infraction infraction, int infractionCount)
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

        var embed = new DiscordEmbedBuilder();

        embed.WithColor(0xFF0000);
        embed.WithTitle(infraction.Type.Humanize());
        embed.WithDescription(string.IsNullOrWhiteSpace(description) ? null : description.FormatSmart(new {user, guild}));
        embed.WithThumbnail(guild.IconUrl);
        embed.WithFooter(guild.Name, guild.IconUrl);
        embed.AddField("Reason", reason);
        embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embed.AddField("Total Infractions", infractionCount, true);

        if (infraction.Type is not InfractionType.Ban or InfractionType.TemporaryBan)
            embed.AddModMailNotice();

        return embed;
    }
}
