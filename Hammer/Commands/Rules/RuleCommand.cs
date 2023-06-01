using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;

namespace Hammer.Commands.Rules;

/// <summary>
///     Represents a class which implements the <c>rule</c> command.
/// </summary>
internal sealed class RuleCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="ruleService">The rule service.</param>
    public RuleCommand(ConfigurationService configurationService, RuleService ruleService)
    {
        _configurationService = configurationService;
        _ruleService = ruleService;
    }

    [SlashCommand("rule", "Displays a rule.")]
    [SlashRequireGuild]
    public async Task RuleAsync(InteractionContext context,
        [Option("rule", "The rule to display.", true), Autocomplete(typeof(RuleAutocompleteProvider))] string search)
    {
        DiscordGuild guild = context.Guild;
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        Rule? rule;

        if (int.TryParse(search, out int ruleId))
        {
            if (!_ruleService.GuildHasRule(guild, ruleId))
            {
                await context.CreateResponseAsync(_ruleService.CreateRuleNotFoundEmbed(guild, ruleId), true);
                return;
            }

            rule = _ruleService.GetRuleById(guild, ruleId)!;
        }
        else
        {
            rule = _ruleService.SearchForRule(guild, search);
            if (rule is null)
            {
                await context.CreateResponseAsync(_ruleService.CreateRuleNotFoundEmbed(guild, search), true);
                return;
            }
        }

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle(string.IsNullOrWhiteSpace(rule.Brief) ? $"Rule #{rule.Id}" : $"Rule #{rule.Id}. {rule.Brief}");
        embed.WithDescription(rule.Description);

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
