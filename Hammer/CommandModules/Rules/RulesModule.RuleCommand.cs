using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("rule")]
    [Description("Displays a specified rule.")]
    [RequireGuild]
    public async Task RuleCommandAsync(CommandContext context, [Description("The ID of the rule to retrieve.")] int id)
    {
        await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
        
        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        
        if (!_ruleService.GuildHasRule(guild, id))
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No such rule");
            embed.WithDescription("A rule by that ID could not be found.");
        }
        else
        {
            Rule rule = _ruleService.GetRuleById(guild, id)!;
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(string.IsNullOrWhiteSpace(rule.Brief) ? $"Rule #{rule.Id}" : $"Rule #{rule.Id}. {rule.Brief}");
            embed.WithDescription(rule.Content);
        }

        await context.RespondAsync(embed);
    }
}
