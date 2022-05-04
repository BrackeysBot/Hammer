using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("add", "Add a rule.", false)]
    [SlashRequireGuild]
    public async Task AddAsync(InteractionContext context,
        [Option("description", "The rule description.")] string description,
        [Option("brief", "The rule brief.")] string? brief = null)
    {
        await context.DeferAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
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
