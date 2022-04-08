using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.API.Plugins;
using BrackeysBot.Core.API;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.API;
using Hammer.CommandModules.Rules;
using Hammer.CommandModules.Staff;
using Hammer.CommandModules.User;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer;

/// <summary>
///     Represents a <see cref="BrackeysBot" /> plugin which handles moderation.
/// </summary>
[Plugin("Hammer")]
[PluginDependencies("BrackeysBot.Core")]
[PluginDescription("A BrackeysBot plugin for managing infractions against misbehaving users.")]
[PluginIntents(DiscordIntents.All)]
public sealed class HammerPlugin : MonoPlugin, IHammerPlugin
{
    private BanService _banService = null!;
    private InfractionService _infractionService = null!;
    private MessageDeletionService _messageDeletionService = null!;
    private MuteService _muteService = null!;
    private WarningService _warningService = null!;

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _banService.BanAsync(user, staffMember, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _banService.BanAsync(user, staffMember, reason);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _banService.TemporaryBanAsync(user, staffMember, null, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _banService.TemporaryBanAsync(user, staffMember, reason, duration);
    }

    /// <inheritdoc />
    public Task DeleteMessageAsync(DiscordMessage message, DiscordMember staffMember, bool notifyAuthor = true)
    {
        return _messageDeletionService.DeleteMessageAsync(message, staffMember, notifyAuthor);
    }

    /// <inheritdoc />
    public IEnumerable<IInfraction> EnumerateInfractions(DiscordGuild guild)
    {
        return _infractionService.EnumerateInfractions(guild);
    }

    /// <inheritdoc />
    public IEnumerable<IInfraction> EnumerateInfractions(DiscordUser user, DiscordGuild guild)
    {
        return _infractionService.EnumerateInfractions(user, guild);
    }

    /// <inheritdoc />
    public int GetInfractionCount(DiscordGuild guild)
    {
        return _infractionService.GetInfractionCount(guild);
    }

    /// <inheritdoc />
    public int GetInfractionCount(DiscordUser user, DiscordGuild guild)
    {
        return _infractionService.GetInfractionCount(user, guild);
    }

    /// <inheritdoc />
    public IReadOnlyList<IInfraction> GetInfractions(DiscordGuild guild)
    {
        return _infractionService.GetInfractions(guild);
    }

    /// <inheritdoc />
    public IReadOnlyList<IInfraction> GetInfractions(DiscordUser user, DiscordGuild guild)
    {
        return _infractionService.GetInfractions(user, guild);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember)
    {
        return await _banService.KickAsync(member, staffMember, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, string reason)
    {
        return await _banService.KickAsync(member, staffMember, reason);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _muteService.MuteAsync(user, staffMember, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _muteService.MuteAsync(user, staffMember, reason);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _muteService.TemporaryMuteAsync(user, staffMember, null, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _muteService.TemporaryMuteAsync(user, staffMember, reason, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _warningService.WarnAsync(user, staffMember, reason);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(PluginManager.GetPlugin<ICorePlugin>()!);

        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<MailmanService>();
        services.AddSingleton<MessageService>();
        services.AddSingleton<MessageDeletionService>();
        services.AddSingleton<WarningService>();

        services.AddHostedSingleton<BanService>();
        services.AddHostedSingleton<InfractionService>();
        services.AddHostedSingleton<MemberNoteService>();
        services.AddHostedSingleton<MessageTrackingService>();
        services.AddHostedSingleton<MessageReportService>();
        services.AddHostedSingleton<MuteService>();
        services.AddHostedSingleton<RuleService>();
        services.AddHostedSingleton<StaffReactionService>();
        services.AddHostedSingleton<UserTrackingService>();
        services.AddHostedSingleton<UserReactionService>();

        services.AddDbContext<HammerContext>();
    }

    /// <inheritdoc />
    protected override async Task OnLoad()
    {
        FetchServices();

        Logger.Info("Creating database");
        var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        await using (AsyncServiceScope scope = scopeFactory.CreateAsyncScope())
        {
            await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
            await context.Database.EnsureCreatedAsync();
        }

        Logger.Info("Registering command modules");
        CommandsNextExtension commandsNext = DiscordClient.GetCommandsNext();
        commandsNext.RegisterCommands<RulesModule>();
        commandsNext.RegisterCommands<StaffModule>();
        commandsNext.RegisterCommands<UserModule>();

        commandsNext.RegisterCommands<BanCommandModule>();
        commandsNext.RegisterCommands<KickCommandModule>();
        commandsNext.RegisterCommands<MuteCommandModule>();
        commandsNext.RegisterCommands<UnbanCommandModule>();
        commandsNext.RegisterCommands<UnmuteCommandModule>();
        commandsNext.RegisterCommands<WarnCommandModule>();

        Logger.Info("Registering InteractivityExtension");
        DiscordClient.UseInteractivity(new InteractivityConfiguration());

        await base.OnLoad();
    }

    private void FetchServices()
    {
        _banService = ServiceProvider.GetRequiredService<BanService>();
        _infractionService = ServiceProvider.GetRequiredService<InfractionService>();
        _messageDeletionService = ServiceProvider.GetRequiredService<MessageDeletionService>();
        _muteService = ServiceProvider.GetRequiredService<MuteService>();
        _warningService = ServiceProvider.GetRequiredService<WarningService>();
    }
}
