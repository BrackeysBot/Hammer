using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Data;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("rule")]
    [Description("Displays a specified rule.")]
    [RequireGuild]
    public async Task RuleCommandAsync(CommandContext context, [Description("The ID of the rule to retrieve.")] int ruleId)
    {
        await context.AcknowledgeAsync();
        DiscordGuild guild = context.Guild;

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            await context.RespondAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId));
            return;
        }

        Rule rule = _ruleService.GetRuleById(guild, ruleId)!;
        
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle(string.IsNullOrWhiteSpace(rule.Brief) ? $"Rule #{rule.Id}" : $"Rule #{rule.Id}. {rule.Brief}");
        embed.WithDescription(rule.Content);

        await context.RespondAsync(embed);
    }
}
