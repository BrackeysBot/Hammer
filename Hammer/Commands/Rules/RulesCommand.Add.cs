using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("add", "Add a rule.", false)]
    [SlashRequireGuild]
    public async Task AddAsync(InteractionContext context,
        [Option("description", "The rule description.")] string description,
        [Option("brief", "The rule brief.")] string? brief = null)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        Rule rule = await _ruleService.AddRuleAsync(guild, description, brief).ConfigureAwait(false);

        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule #{rule.Id} added");
        if (string.IsNullOrWhiteSpace(brief))
            embed.WithDescription(rule.Description);
        else
            embed.AddField(rule.Brief, rule.Description);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
