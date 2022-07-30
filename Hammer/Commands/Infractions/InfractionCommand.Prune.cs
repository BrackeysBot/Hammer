using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("prune", "Prune all stale infractions for invalid users.", false)]
    [SlashRequireGuild]
    public async Task PruneAsync(InteractionContext context)
    {
        await context.DeferAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Prune infractions?");
        embed.WithDescription("All stale infractions (infractions for users which no longer exist) " +
                              "will be pruned from the database. This action cannot be undone. Are you sure you want to proceed?");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);

        var yes = new DiscordButtonComponent(ButtonStyle.Success, "prune-confirm", "Yes");
        var no = new DiscordButtonComponent(ButtonStyle.Danger, "prune-cancel", "No");
        builder.AddComponents(yes, no);

        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);

        InteractivityResult<ComponentInteractionCreateEventArgs> result =
            await message.WaitForButtonAsync(i => i.User == context.User).ConfigureAwait(false);

        builder.Clear();
        builder.ClearComponents();
        builder.AddEmbed(embed);

        if (result.TimedOut)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Prune cancelled");
            embed.WithDescription("The pruning process was cancelled because no action was taken.");
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        embed.WithColor(DiscordColor.Blurple);
        embed.WithTitle("Prune in progress");
        embed.WithDescription("Please wait while stale infractions are being pruned. This process may take several minutes.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);

        int count = await _infractionService.PruneStaleInfractionsAsync().ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Prune complete");
        embed.WithDescription($"{count} stale infractions were pruned.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
