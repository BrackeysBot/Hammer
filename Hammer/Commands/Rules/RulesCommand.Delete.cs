using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Configuration;
using Hammer.Extensions;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("delete", "Deletes a rule.", false)]
    [SlashRequireGuild]
    public async Task DeleteAsync(InteractionContext context,
        [Autocomplete(typeof(RuleAutocompleteProvider))] [Option("rule", "The rule to modify")] long ruleId)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;
        var builder = new DiscordWebhookBuilder();

        if (!_ruleService.GuildHasRule(guild, (int) ruleId))
        {
            builder.AddEmbed(_ruleService.CreateRuleNotFoundEmbed(guild, (int) ruleId));
            await context.EditResponseAsync(builder);
            return;
        }

        _ruleService.DeleteRule(guild, (int)ruleId);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {ruleId} deleted");
        embed.WithDescription("To view the new rules, use the `/rules` command.");

        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
