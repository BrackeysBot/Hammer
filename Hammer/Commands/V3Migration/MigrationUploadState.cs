using System.Text.Json;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.Data.v3_compat;
using Hammer.Interactivity;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationUploadState : ConversationState
{
    private readonly bool _fullMigration;
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationUploadState" /> class.
    /// </summary>
    /// <param name="fullMigration">Whether or not to perform a full migration.</param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationUploadState(bool fullMigration, Conversation conversation)
        : base(conversation)
    {
        _fullMigration = fullMigration;
        _httpClient = Conversation.Services.GetRequiredService<HttpClient>();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0x007EC6);
        embed.WithThumbnail(context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png));
        embed.WithTitle("💾 Upload v3 database");
        embed.WithDescription("Please attach your v3 infraction database file. This will be a file called `users.json`.\n\n" +
                              "If no file is attached within 60 seconds, the process will be cancelled");

        var builder = new DiscordWebhookBuilder();
        builder.Clear();
        builder.AddEmbed(embed);
        await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);

        InteractivityResult<DiscordMessage> userResponse =
            await context.Channel.GetNextMessageAsync(message =>
                    message.Author == context.User &&
                    message.Attachments.Any(a => Path.GetExtension(a.FileName)
                        .Equals(".json", StringComparison.OrdinalIgnoreCase)),
                TimeSpan.FromSeconds(60));

        if (userResponse.TimedOut)
        {
            embed.WithThumbnail(string.Empty);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("🛑 Migration cancelled");
            embed.WithDescription("You took too long to upload your database. Please try again.");

            builder.Clear();
            builder.AddEmbed(embed);
            await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);
            return null;
        }

        DiscordAttachment attachment =
            userResponse.Result.Attachments.First(a =>
                Path.GetExtension(a.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase));

        try
        {
            await using Stream stream = await _httpClient.GetStreamAsync(attachment.Url, cancellationToken).ConfigureAwait(false);
            UsersModel? users = await JsonSerializer.DeserializeAsync<UsersModel>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new MigrationConfirmState(users?.Users ?? new List<UserData>(), _fullMigration, Conversation);
        }
        catch (JsonException)
        {
            return new MigrationInvalidJsonState(_fullMigration, Conversation);
        }
        catch (TaskCanceledException)
        {
            return new MigrationCanceledState(Conversation);
        }
        finally
        {
            await userResponse.Result.DeleteAsync().ConfigureAwait(false);
        }
    }
}
