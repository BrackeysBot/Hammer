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
        await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
        
        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No such rule");
            embed.WithDescription("A rule by that ID could not be found.");
        }
        else
        {
            await _ruleService.SetRuleContentAsync(guild, ruleId, ruleContent);
            embed.WithColor(0x4CAF50);
            embed.WithTitle($"Rule {ruleId} updated");
            embed.WithDescription(ruleContent);
        }
        
        await context.RespondAsync(embed);
    }
}
