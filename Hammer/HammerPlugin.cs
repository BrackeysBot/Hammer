using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.API.Plugins;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Hammer.API;
using Hammer.CommandModules;
using Hammer.CommandModules.Infractions;
using Hammer.CommandModules.Rules;
using Hammer.CommandModules.Staff;
using Hammer.CommandModules.User;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

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
    private RuleService _ruleService = null!;
    private WarningService _warningService = null!;

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _banService.BanAsync(user, staffMember, null, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.BanAsync(user, staffMember, null, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _banService.BanAsync(user, staffMember, reason, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.BanAsync(user, staffMember, reason, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _banService.TemporaryBanAsync(user, staffMember, null, duration, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.TemporaryBanAsync(user, staffMember, null, duration, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _banService.TemporaryBanAsync(user, staffMember, reason, duration, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> BanAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration,
        int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.TemporaryBanAsync(user, staffMember, reason, duration, rule).ConfigureAwait(false);
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
    public int GetInfractionCount(ulong userId, ulong guildId)
    {
        return _infractionService.GetInfractionCount(userId, guildId);
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
    public bool IsUserMuted(DiscordUser user, DiscordGuild guild)
    {
        return _muteService.IsUserMuted(user, guild);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember)
    {
        return await _banService.KickAsync(member, staffMember, null, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.KickAsync(member, staffMember, null, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, string reason)
    {
        return await _banService.KickAsync(member, staffMember, reason, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> KickAsync(DiscordMember member, DiscordMember staffMember, string reason, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _banService.KickAsync(member, staffMember, reason, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await _muteService.MuteAsync(user, staffMember, null, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _muteService.MuteAsync(user, staffMember, null, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _muteService.MuteAsync(user, staffMember, reason, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _muteService.MuteAsync(user, staffMember, reason, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration)
    {
        return await _muteService.TemporaryMuteAsync(user, staffMember, null, duration, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, TimeSpan duration, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _muteService.TemporaryMuteAsync(user, staffMember, null, duration, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration)
    {
        return await _muteService.TemporaryMuteAsync(user, staffMember, reason, duration, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string reason, TimeSpan duration,
        int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _muteService.TemporaryMuteAsync(user, staffMember, reason, duration, rule).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IInfraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        return await _warningService.WarnAsync(user, staffMember, reason, null).ConfigureAwait(false);
    }

    public async Task<IInfraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason, int ruleBroken)
    {
        Rule? rule = _ruleService.GetRuleById(staffMember.Guild, ruleBroken);
        return await _warningService.WarnAsync(user, staffMember, reason, rule).ConfigureAwait(false);
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
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }

        Logger.Info("Registering command modules");
        CommandsNextExtension commandsNext = DiscordClient.GetCommandsNext();
        commandsNext.RegisterCommands<RulesModule>();
        commandsNext.RegisterCommands<StaffModule>();
        commandsNext.RegisterCommands<UserModule>();

        commandsNext.RegisterCommands<UnbanCommandModule>();

        Logger.Info("Registering slash commands");
        SlashCommandsExtension slashCommands = DiscordClient.GetSlashCommands();
        slashCommands.RegisterCommands<BanCommand>();
        slashCommands.RegisterCommands<InfractionCommand>();
        slashCommands.RegisterCommands<KickCommand>();
        slashCommands.RegisterCommands<MuteCommand>();
        slashCommands.RegisterCommands<UnmuteCommand>();
        slashCommands.RegisterCommands<WarnCommand>();

        Logger.Info("Registering InteractivityExtension");
        DiscordClient.UseInteractivity(new InteractivityConfiguration());
        DiscordClient.AutoJoinThreads();
        DiscordClient.GuildAvailable += DiscordClientOnGuildAvailable;

        var corePlugin = ServiceProvider.GetRequiredService<ICorePlugin>();
        corePlugin.RegisterUserInfoField(builder =>
        {
            bool ExecutionCheck(UserInfoFieldContext context)
            {
                if (context.Member is not { } member) return false;

                DiscordGuild guild = context.Guild!;
                if (member != context.TargetMember && member.GetPermissionLevel(guild) < PermissionLevel.Guru)
                    return false;

                return _infractionService.GetInfractionCount(context.TargetUser, guild) > 0;
            }

            builder.WithName("Infractions");
            builder.WithValue(context => _infractionService.GetInfractionCount(context.TargetUser, context.Guild!));
            builder.WithExecutionFilter(ExecutionCheck);
        });

        await base.OnLoad().ConfigureAwait(false);
    }

    private void FetchServices()
    {
        _banService = ServiceProvider.GetRequiredService<BanService>();
        _infractionService = ServiceProvider.GetRequiredService<InfractionService>();
        _messageDeletionService = ServiceProvider.GetRequiredService<MessageDeletionService>();
        _muteService = ServiceProvider.GetRequiredService<MuteService>();
        _ruleService = ServiceProvider.GetRequiredService<RuleService>();
        _warningService = ServiceProvider.GetRequiredService<WarningService>();
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Logger.Info($"Registering slash commands for {e.Guild}");
        SlashCommandsExtension slashCommands = sender.GetSlashCommands();
        slashCommands.RegisterCommands<ReportMessageApplicationCommand>(e.Guild.Id);
        return slashCommands.RefreshCommands();
    }
}
