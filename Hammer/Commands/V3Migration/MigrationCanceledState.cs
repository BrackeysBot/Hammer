using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Extensions;
using Hammer.Interactivity;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationCanceledState : ConversationState
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationCanceledState" /> class.
    /// </summary>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationCanceledState(Conversation conversation) : base(conversation)
    {
        _configurationService = conversation.Services.GetRequiredService<ConfigurationService>();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbedBuilder();
        if (_configurationService.TryGetGuildConfiguration(context.Guild!, out GuildConfiguration? guildConfiguration))
            embed = context.Guild!.CreateDefaultEmbed(guildConfiguration, false);

        embed.WithColor(DiscordColor.Red);
        embed.WithTitle("Migration Cancelled");
        embed.WithDescription("The migration process has been cancelled.\n\n" +
                              "If you wish to start a new migration, please use the `migrate` command again.");

        var builder = new DiscordWebhookBuilder();
        builder.Clear();
        builder.AddEmbed(embed);

        await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);
        return null;
    }
}
