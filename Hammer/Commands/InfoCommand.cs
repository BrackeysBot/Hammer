using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Extensions;
using Hammer.Services;
using Humanizer;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the <c>info</c> command.
/// </summary>
internal sealed class InfoCommand : ApplicationCommandModule
{
    private readonly BotService _botService;
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoCommand" /> class.
    /// </summary>
    /// <param name="botService">The bot service.</param>
    /// <param name="configurationService">The configuration service.</param>
    public InfoCommand(BotService botService, ConfigurationService configurationService)
    {
        _botService = botService;
        _configurationService = configurationService;
    }

    [SlashCommand("info", "Displays information about the bot.")]
    [SlashRequireGuild]
    public async Task InfoAsync(InteractionContext context)
    {
        DiscordGuild guild = context.Guild;
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        DiscordClient client = context.Client;
        DiscordMember member = (await client.CurrentUser.GetAsMemberOfAsync(guild).ConfigureAwait(false))!;
        string hammerVersion = _botService.Version;
        DiscordColor embedColor = member.Color;
        if (embedColor.Value == 0)
        {
            embedColor = configuration.PrimaryColor;
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(member);
        embed.WithColor(embedColor);
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithTitle($"Hammer v{hammerVersion}");
        embed.AddField("Ping", client.Ping, true);
        embed.AddField("Uptime", (DateTimeOffset.UtcNow - _botService.StartedAt).Humanize(), true);
        embed.AddField("View Source", "[View on GitHub](https://github.com/BrackeysBot/Hammer/)", true);

        var builder = new StringBuilder();
        builder.AppendLine($"Hammer: {hammerVersion}");
        builder.AppendLine($"D#+: {client.VersionString}");
        builder.AppendLine($"Gateway: {client.GatewayVersion}");
        builder.AppendLine($"CLR: {Environment.Version.ToString(3)}");
        builder.AppendLine($"Host: {Environment.OSVersion}");

        embed.AddField("Version", Formatter.BlockCode(builder.ToString()));

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
