using System.Diagnostics.CodeAnalysis;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Humanizer;
using Microsoft.Extensions.Hosting;
using NLog;
using X10D.DSharpPlus;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles infraction cooldowns, to prevent duplicate issues.
/// </summary>
internal sealed class InfractionCooldownService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<Infraction, DateTimeOffset> _hotInfractions = new();
    private readonly Timer _cooldownTimer = new();

    /// <summary>
    ///     Returns a value indicating whether the specified user has recently received an infraction.
    /// </summary>
    /// <param name="user">The user whose cooldown status to retrieve.</param>
    /// <param name="staffMember">The staff member who is initiating the infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="user" /> is on cooldown; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="user" /> is <see langword="null" />.</exception>
    public bool IsCooldownActive(DiscordUser user, DiscordMember staffMember)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(staffMember);

        lock (_hotInfractions)
        {
            foreach ((Infraction infraction, _) in _hotInfractions.OrderByDescending(p => p.Value))
            {
                if (infraction.UserId == user.Id)
                    return infraction.StaffMemberId != staffMember.Id;
            }
        }

        return false;
    }

    /// <summary>
    ///     Shows a confirmation prompt to the user to confirm the infraction.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="user">The user receiving the infraction.</param>
    /// <param name="infraction">The recently-issued infraction.</param>
    /// <param name="infractionEmbed">The infraction embed to display.</param>
    /// <returns><see langword="true" /> if the user confirmed the infraction; otherwise, <see langword="false" />.</returns>
    public async Task<bool> ShowConfirmationAsync(
        InteractionContext context,
        DiscordUser user,
        Infraction infraction,
        DiscordEmbed infractionEmbed
    )
    {
        string content = $"Hold on! This may be a duplicate. {user.Mention} was {GetInfractionVerb(infraction.Type)} " +
                         $"{infraction.IssuedAt.Humanize()} by {MentionUtility.MentionUser(infraction.StaffMemberId)} " +
                         "(see details below).\nPlease confirm whether or not you'd like to proceed with the infraction.";

        var builder = new DiscordWebhookBuilder();
        builder.WithContent(content);
        builder.AddEmbed(infractionEmbed);

        var proceed = new DiscordButtonComponent(ButtonStyle.Success, "infr-proceed", "Proceed");
        var cancel = new DiscordButtonComponent(ButtonStyle.Danger, "infr-cancel", "Cancel");
        builder.AddComponents(proceed, cancel);

        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);
        var result = await message.WaitForButtonAsync(TimeSpan.FromMinutes(1)).ConfigureAwait(false);

        if (result.TimedOut)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Infraction cancelled");
            embed.WithDescription("Confirmation took too long. No action was performed on the user.");
            await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
            return false;
        }

        switch (result.Result.Id)
        {
            case "infr-proceed":
                return true;

            case "infr-cancel":
                var embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Red);
                embed.WithTitle("Infraction cancelled");
                embed.WithDescription("Infraction has been cancelled. No action was performed on the user.");
                await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
                return false;

            default:
                // heuristically unreachable but required for compiler
                return false;
        }
    }

    /// <summary>
    ///     Starts the cooldown for the specified infraction.
    /// </summary>
    /// <param name="infraction">The infraction whose cooldown to initiate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="infraction" /> is already on a cooldown.</exception>
    public void StartCooldown(Infraction infraction)
    {
        ArgumentNullException.ThrowIfNull(infraction);

        lock (_hotInfractions)
        {
            if (_hotInfractions.ContainsKey(infraction))
                throw new InvalidOperationException("Infraction is already on cooldown.");

            foreach (Infraction current in _hotInfractions.Keys.ToArray())
            {
                if (current.UserId == infraction.UserId)
                    _hotInfractions.Remove(current);
            }

            _hotInfractions.Add(infraction, DateTimeOffset.Now);
        }
    }

    /// <summary>
    ///     Stops the cooldown for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user whose cooldown to stop.</param>
    public void StopCooldown(ulong userId)
    {
        lock (_hotInfractions)
        {
            foreach (Infraction infraction in _hotInfractions.Keys.ToArray())
            {
                if (infraction.UserId == userId)
                    _hotInfractions.Remove(infraction);
            }
        }
    }

    /// <summary>
    ///     Stops the cooldown for the specified user.
    /// </summary>
    /// <param name="user">The user whose cooldown to stop.</param>
    /// <exception cref="ArgumentNullException"><paramref name="user" /> is <see langword="null" />.</exception>
    public void StopCooldown(DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_hotInfractions)
        {
            foreach (Infraction infraction in _hotInfractions.Keys.ToArray())
            {
                if (infraction.UserId == user.Id)
                    _hotInfractions.Remove(infraction);
            }
        }
    }

    /// <summary>
    ///     Attempts to get the hot infraction for a specified user, and returns a value indicating whether a cooldown is active.
    /// </summary>
    /// <param name="user">The whose hot infraction to retrieve.</param>
    /// <param name="infraction">
    ///     When this method returns, contains the hot infraction for <paramref name="user" />, or <see langword="null" /> if no
    ///     cooldown is active.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if a hot infraction exists for <paramref name="user" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetInfraction(DiscordUser user, [NotNullWhen(true)] out Infraction? infraction)
    {
        infraction = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (user is null) return false;

        lock (_hotInfractions)
        {
            foreach ((Infraction current, _) in _hotInfractions)
            {
                if (current.UserId == user.Id)
                {
                    infraction = current;
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cooldownTimer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
        _cooldownTimer.Elapsed += CooldownTimer_Elapsed;
        _cooldownTimer.Start();

        return Task.CompletedTask;
    }

    private void CooldownTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_hotInfractions)
        {
            foreach ((Infraction? infraction, DateTimeOffset cooldownStart) in _hotInfractions.ToArray())
            {
                if (DateTimeOffset.Now - cooldownStart > TimeSpan.FromMinutes(30))
                {
                    Logger.Info($"Cooldown expired for user {infraction.UserId} - removing");
                    StopCooldown(infraction.UserId);
                }
            }
        }
    }

    private static string GetInfractionVerb(InfractionType type)
    {
        return type switch
        {
            InfractionType.Warning => "warned",
            InfractionType.Mute => "muted",
            InfractionType.Kick => "kicked",
            InfractionType.Ban => "banned",
            InfractionType.TemporaryBan => "temporarily banned",
            InfractionType.TemporaryMute => "temporarily muted",
            _ => "punished" // heuristically unreachable but compiler Daddy is angry
        };
    }
}
