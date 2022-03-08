using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Attributes;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.CommandModules.Rules;

internal sealed partial class RulesModule
{
    [Command("addrule")]
    [Aliases("newrule")]
    [Description("Adds a new rule.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task AddRuleCommandAsync(CommandContext context,
        [Description("The new rule context."), RemainingText] string ruleContent)
    {
        await context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));

        DiscordGuild guild = context.Guild;
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        Rule rule = await _ruleService.AddRuleAsync(guild, ruleContent);
        
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule #{rule.Id} added");
        embed.WithDescription(rule.Content);

        await context.RespondAsync(embed);
    }
}
