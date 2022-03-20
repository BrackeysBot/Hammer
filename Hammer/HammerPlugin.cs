using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrackeysBot.API.Plugins;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Hammer.API;
using Hammer.CommandModules.Rules;
using Hammer.CommandModules.Staff;
using Hammer.CommandModules.User;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hammer;

/// <summary>
///     Represents a <see cref="BrackeysBot" /> plugin which handles moderation.
/// </summary>
[Plugin("Hammer", "1.0.0")]
[PluginDependencies("BrackeysBot.Core")]
[PluginDescription("A BrackeysBot plugin for managing infractions against misbehaving users.")]
[PluginIntents(DiscordIntents.All)]
public sealed class HammerPlugin : MonoPlugin, IHammerPlugin
{
    private InfractionService _infractionService = null!;


    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _infractionService.BanAsync(user, staffMember, null, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _infractionService.BanAsync(user, staffMember, reason, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _infractionService.BanAsync(user, staffMember, null, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _infractionService.BanAsync(user, staffMember, reason, duration);
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
        return await _infractionService.KickAsync(member, staffMember, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, string reason)
    {
        return await _infractionService.KickAsync(member, staffMember, reason);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _infractionService.MuteAsync(user, staffMember, null, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _infractionService.MuteAsync(user, staffMember, reason, null);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _infractionService.MuteAsync(user, staffMember, null, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _infractionService.MuteAsync(user, staffMember, reason, duration);
    }

    /// <inheritdoc />
    public async Task<IInfraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _infractionService.WarnAsync(user, staffMember, reason);
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

        Logger.Info("Registering InteractivityExtension");
        DiscordClient.UseInteractivity(new InteractivityConfiguration());

        await base.OnLoad();
    }

    private void FetchServices()
    {
        _infractionService = ServiceProvider.GetRequiredService<InfractionService>();
    }
}
