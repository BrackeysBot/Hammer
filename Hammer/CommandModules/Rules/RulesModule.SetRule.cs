using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("setrule")]
    [Aliases("editrule", "modifyrule", "changerule")]
    [Description("Modifies a rule text.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task SetRuleCommandAsync(CommandContext context,
        [Description("The ID of the rule to remove.")]
        int ruleId,
        [Description("The new rule text")] [RemainingText]
        string ruleContent)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);
        DiscordGuild guild = context.Guild;

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            await context.RespondAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId)).ConfigureAwait(false);
            return;
        }

        await _ruleService.SetRuleContentAsync(guild, ruleId, ruleContent);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} updated");
        embed.WithDescription(ruleContent);

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
