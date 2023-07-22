using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Hammer.Extensions;
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

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailmanService" /> class.
    /// </summary>
    public MailmanService(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Sends an infraction notice to the applicable member, if possible.
    /// </summary>
    /// <param name="infraction">The infraction to notify.</param>
    /// <param name="infractionCount">The infraction count to display on the embed.</param>
    /// <param name="options">The infraction options.</param>
    /// <returns>The message which was sent to the member, or <see langword="null" /> if the message could not be sent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> SendInfractionAsync(Infraction infraction, int infractionCount, InfractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(infraction);

        if (!_discordClient.Guilds.TryGetValue(infraction.GuildId, out DiscordGuild? guild)) return null;

        DiscordMember? member = await guild.GetMemberOrNullAsync(infraction.UserId).ConfigureAwait(false);
        if (member is null) return null; // bots can only DM members

        try
        {
            DiscordEmbed? embed = CreatePrivateEmbed(infraction, infractionCount, options, member);
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

    private DiscordEmbed? CreatePrivateEmbed(Infraction infraction, int count, InfractionOptions options, DiscordMember? member)
    {
        if (member is null) return null;
        if (!_discordClient.Guilds.TryGetValue(infraction.GuildId, out DiscordGuild? guild)) return null;

        string? description = infraction.Type.GetEmbedMessage();
        string reason = infraction.Reason.WithWhiteSpaceAlternative(Formatter.Italic("No reason given."));
        var embed = new DiscordEmbedBuilder();

        embed.WithColor(0xFF0000);
        embed.WithTitle(infraction.Type.Humanize());
        embed.WithDescription(string.IsNullOrWhiteSpace(description)
            ? null
            : description.FormatSmart(new {user = member, guild}));
        embed.WithThumbnail(guild.IconUrl);
        embed.WithFooter(guild.Name, guild.IconUrl);
        embed.AddField("Reason", reason);
        embed.AddFieldIf(infraction.RuleId.HasValue, "Rule Broken", () => $"{infraction.RuleId} - {infraction.RuleText}", true);

        switch (infraction.Type)
        {
            case InfractionType.Warning:
                embed.AddField("Punishment", "**WARNING**", true);
                break;
            case InfractionType.Kick:
                embed.AddField("Punishment", "**KICK**", true);
                break;
            case InfractionType.Mute or InfractionType.TemporaryMute:
                embed.AddField("Punishment", $"**MUTE**\n{options.ReadableDuration}", true);
                break;
            case InfractionType.Ban or InfractionType.TemporaryBan:
                embed.AddField("Punishment", $"**BAN**\n{options.ReadableDuration}", true);
                break;
        }

        embed.AddField("Total Infractions", count, true);

        if (infraction.Type is not InfractionType.Ban or InfractionType.TemporaryBan)
            embed.AddModMailNotice();

        return embed;
    }
}
