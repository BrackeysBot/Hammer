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
    [Command("deleterule")]
    [Aliases("removerule", "delrule", "remrule", "rmrule")]
    [Description("Deletes a rule.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task DeleteRuleCommandAsync(CommandContext context, [Description("The ID of the rule to remove.")] int ruleId)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);
        DiscordGuild guild = context.Guild;

        if (!_ruleService.GuildHasRule(guild, ruleId))
        {
            await context.RespondAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId));
            return;
        }

        await _ruleService.DeleteRuleAsync(guild, ruleId).ConfigureAwait(false);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} deleted");
        embed.WithDescription($"To view the new rules, run `{context.Prefix}rules`");

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
