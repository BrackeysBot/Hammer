using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("delete", "Deletes a rule.", false)]
    [SlashRequireGuild]
    public async Task DeleteAsync(InteractionContext context,
        [Autocomplete(typeof(RuleAutocompleteProvider))] [Option("rule", "The rule to modify")] long ruleId)
    {
        await context.DeferAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;
        var builder = new DiscordWebhookBuilder();

        if (!_ruleService.GuildHasRule(guild, (int) ruleId))
        {
            builder.AddEmbed(_ruleService.CreateRuleNotFoundEmbed(guild, (int) ruleId));
            await context.EditResponseAsync(builder);
            return;
        }

        await _ruleService.DeleteRuleAsync(guild, (int) ruleId).ConfigureAwait(false);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} deleted");
        embed.WithDescription("To view the new rules, use the `/rules` command.");

        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
