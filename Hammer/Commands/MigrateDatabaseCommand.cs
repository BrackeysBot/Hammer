using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using Microsoft.Extensions.Logging;

namespace Hammer.Commands;

internal sealed class MigrateDatabaseCommand : ApplicationCommandModule
{
    private readonly ILogger<MigrateDatabaseCommand> _logger;
    private readonly DatabaseService _databaseService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrateDatabaseCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="databaseService">The database service.</param>
    public MigrateDatabaseCommand(ILogger<MigrateDatabaseCommand> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    [SlashCommand("migratedb", "Migrates the SQLite database to MySQL/MariaDB.", false)]
    [SlashRequireGuild]
    public async Task MigrateDatabaseAsync(InteractionContext context)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("⏳ Migration in progress");
        embed.WithDescription("Please wait while the database is migrated...");

        await context.CreateResponseAsync(embed);
        try
        {
            int rows = await _databaseService.MigrateAsync();

            var builder = new DiscordWebhookBuilder();
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle("✅ Migration complete");
            embed.WithDescription($"All data has been successfully migrated. {rows:N0} rows affected");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to migrate");
            var builder = new DiscordWebhookBuilder();
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle($"⚠️ Migration failed: {exception.GetType().Name}");
            embed.WithDescription($"{exception.GetType().Name} was thrown during migration: {exception.Message}\n\n" +
                                  "View the log for more details");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder);
        }
    }
}
