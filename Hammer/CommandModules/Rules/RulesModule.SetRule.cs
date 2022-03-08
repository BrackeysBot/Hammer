﻿using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Attributes;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("setrule")]
    [Aliases("editrule", "modifyrule", "changerule")]
    [Description("Modifies a rule text.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task SetRuleCommandAsync(CommandContext context,
        [Description("The ID of the rule to remove.")] int ruleId,
        [Description("The new rule text"), RemainingText] string ruleContent)
    {
        await context.AcknowledgeAsync();
        DiscordGuild guild = context.Guild;

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            await context.RespondAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId));
            return;
        }
        
        await _ruleService.SetRuleContentAsync(guild, ruleId, ruleContent);
        
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} updated");
        embed.WithDescription(ruleContent);

        await context.RespondAsync(embed);
    }
}
