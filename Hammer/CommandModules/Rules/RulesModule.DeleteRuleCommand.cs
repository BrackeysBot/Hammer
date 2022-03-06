using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Attributes;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("deleterule")]
    [Aliases("removerule", "delrule", "remrule", "rmrule")]
    [Description("Deletes a rule.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task DeleteRuleCommandAsync(CommandContext context, [Description("The ID of the rule to remove.")] int ruleId)
    {
        await context.AcknowledgeAsync();
        DiscordGuild guild = context.Guild;

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            await context.RespondAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId));
            return;
        }
        
        await _ruleService.DeleteRuleAsync(guild, ruleId);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} deleted");
        embed.WithDescription($"To view the new rules, run `{context.Prefix}rules`");

        await context.RespondAsync(embed);
    }
}
