using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hammer.Data;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("addrule")]
    [Aliases("newrule")]
    [Description("Adds a new rule.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task AddRuleCommandAsync(CommandContext context,
        [Description("The new rule context.")] [RemainingText]
        string ruleContent)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        Rule rule = await _ruleService.AddRuleAsync(guild, ruleContent).ConfigureAwait(false);

        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule #{rule.Id} added");
        embed.WithDescription(rule.Content);

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
