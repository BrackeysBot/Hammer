using System.Collections.Generic;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hammer.Data;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("rules")]
    [Description("Displays the server rules.")]
    [RequireGuild]
    public async Task RulesCommandAsync(CommandContext context)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);
        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);

        IReadOnlyList<Rule> rules = _ruleService.GetGuildRules(guild);

        if (rules.Count == 0)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No Rules Found");
            embed.WithDescription($"No rules could be found for {Formatter.Bold(guild.Name)}.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Server Rules");
            foreach (Rule rule in rules)
            {
                string title = string.IsNullOrWhiteSpace(rule.Brief) ? $"Rule #{rule.Id}" : $"Rule #{rule.Id}. {rule.Brief}";
                embed.AddField(title, rule.Content);
            }
        }

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
