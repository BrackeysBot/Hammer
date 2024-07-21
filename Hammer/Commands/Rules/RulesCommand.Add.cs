using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Interactivity;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("add", "Add a rule.", false)]
    [SlashRequireGuild]
    public async Task AddAsync(InteractionContext context)
    {
        DiscordGuild guild = context.Guild;

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true);
            return;
        }

        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Add Rule");
        DiscordModalTextInput brief = modal.AddInput("Brief Description",
            "e.g. Be respectful",
            isRequired: false);
        DiscordModalTextInput description = modal.AddInput("Description",
            "e.g. Please treat other members with respect. Refrain from verbal insults and attacks.",
            isRequired: true,
            inputStyle: TextInputStyle.Paragraph);

        DiscordModalResponse response =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5));

        if (response == DiscordModalResponse.Success)
        {
            Rule rule = _ruleService.AddRule(guild, description.Value!, brief.Value);
            DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle($"Rule #{rule.Id} added");
            if (string.IsNullOrWhiteSpace(brief.Value))
                embed.WithDescription(rule.Description);
            else
                embed.AddField(rule.Brief, rule.Description);

            var webhook = new DiscordWebhookBuilder();
            webhook.AddEmbed(embed);
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }
    }
}
