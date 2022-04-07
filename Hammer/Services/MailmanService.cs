using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.API;
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

    /// <summary>
    ///     Initializes a new instance of the <see cref="MailmanService" /> class.
    /// </summary>
    public MailmanService(HammerPlugin hammerPlugin)
    {
        _hammerPlugin = hammerPlugin;
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
            member = await infraction.Guild.GetMemberAsync(infraction.User.Id);
        }
        catch (NotFoundException)
        {
            // bots can only DM users who are in the guild
            return null;
        }

        try
        {
            DiscordEmbed embed = CreatePrivateInfractionEmbed(infraction);
            return await member.SendMessageAsync(embed);
        }
        catch (UnauthorizedException)
        {
            // bot is blocked or DMs disabled
            return null;
        }
    }

    private DiscordEmbed CreatePrivateInfractionEmbed(IInfraction infraction)
    {
        if (infraction.Type == InfractionType.Gag)
            throw new ArgumentException(ExceptionMessages.NoEmbedForGag, nameof(infraction));

        DiscordUser user = infraction.User;
        DiscordGuild guild = infraction.Guild;
        int infractionCount = _hammerPlugin.GetInfractionCount(user, guild);

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

        return new DiscordEmbedBuilder()
            .WithColor(0xFF0000)
            .WithTitle(infraction.Type.Humanize())
            .WithDescription(string.IsNullOrWhiteSpace(description) ? null : description.FormatSmart(new {user, guild}))
            .WithThumbnail(guild.IconUrl)
            .WithFooter(guild.Name, guild.IconUrl)
            .AddField(EmbedFieldNames.Reason, reason)
            .AddField(EmbedFieldNames.TotalInfractions, infractionCount);
    }
}
