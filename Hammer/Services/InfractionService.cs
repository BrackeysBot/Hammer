using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Hammer.API;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Data.Infractions;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles and manipulates infractions.
/// </summary>
/// <seealso cref="TemporaryMuteService" />
internal sealed class InfractionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigurationService _configurationService;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly Timer _gagTimer = new();
    private readonly Dictionary<DiscordGuild, List<Infraction>> _infractionCache = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TemporaryMuteService _temporaryMuteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionService" /> class.
    /// </summary>
    public InfractionService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient,
        ConfigurationService configurationService, TemporaryMuteService temporaryMuteService)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _temporaryMuteService = temporaryMuteService;
        _corePlugin = corePlugin;

        _gagTimer.Interval = 1000;
        _gagTimer.Start();
    }

    /// <summary>
    ///     Adds an infraction to the database.
    /// </summary>
    /// <param name="infraction">The infraction to add.</param>
    /// <returns>The infraction entity.</returns>
    /// <remarks>
    ///     Do NOT use this method to issue infractions to users. Use an appropriate user-targeted method such as
    ///     <see cref="BanAsync" />, <see cref="GagAsync" />, <see cref="KickAsync" />, <see cref="WarnAsync" />, or a silencing
    ///     action from <see cref="TemporaryMuteService" />.
    /// </remarks>
    /// <seealso cref="CreateInfractionAsync" />
    /// <seealso cref="BanAsync" />
    /// <seealso cref="GagAsync" />
    /// <seealso cref="KickAsync" />
    /// <seealso cref="WarnAsync" />
    /// <seealso cref="TemporaryMuteService" />
    public async Task<Infraction> AddInfractionAsync(Infraction infraction, DiscordGuild guild)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        infraction = (await context.AddAsync(infraction)).Entity;
        await context.SaveChangesAsync();

        if (!_infractionCache.TryGetValue(guild, out List<Infraction>? infractions))
        {
            infractions = new List<Infraction>();
            _infractionCache.Add(guild, infractions);
        }

        infractions.Add(infraction);
        return infraction;
    }

    /// <summary>
    ///     Adds infractions to the database.
    /// </summary>
    /// <param name="infractions">The infractions to add.</param>
    /// <remarks>Do NOT use this method to issue infractions to users. Use an appropriate user-targeted method.</remarks>
    /// <seealso cref="WarnAsync" />
    public async Task AddInfractionsAsync(IEnumerable<Infraction> infractions)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.AddRangeAsync(infractions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     <para>Creates an infraction from the specified options, notifying the target user if:</para>
    ///     <para>1) the user is currently in the same guild as the staff member issuing the infraction</para>
    ///     -and-
    ///     <para>2) the infraction type is not <see cref="InfractionType.Gag" />.</para>
    ///     -and-
    ///     <para>
    ///         3) <see cref="InfractionOptions.NotifyUser" /> as defined in <paramref name="options" /> is
    ///         <see langword="true" />.
    ///     </para>
    /// </summary>
    /// <param name="type">The infraction type.</param>
    /// <param name="user">The user receiving this infraction.</param>
    /// <param name="staffMember">The staff member issuing the infraction.</param>
    /// <param name="options">
    ///     An instance of <see cref="InfractionOptions" /> containing additional information regarding the infraction.
    /// </param>
    /// <returns>The newly-created infraction.</returns>
    public async Task<Infraction> CreateInfractionAsync(InfractionType type, DiscordUser user, DiscordMember staffMember,
        InfractionOptions options)
    {
        string? reason = options.Reason.AsNullIfWhiteSpace();

        DiscordGuild guild = staffMember.Guild;
        DateTimeOffset? expirationTime = options.ExpirationTime;

        if (type == InfractionType.Gag)
        {
            GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
            expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(guildConfiguration.MuteConfiguration.GagDuration);
        }

        Infraction infraction = await AddInfractionAsync(new Infraction
        {
            IssuedAt = DateTimeOffset.UtcNow,
            UserId = user.Id,
            GuildId = guild.Id,
            StaffMemberId = staffMember.Id,
            Reason = reason,
            Type = type,
            ExpirationTime = expirationTime
        }, guild);

        var logMessageBuilder = new StringBuilder();
        logMessageBuilder.AppendLine($"{type.ToString("G")} issued to {user} by {staffMember} in {guild}");
        logMessageBuilder.AppendLine($"Reason: {reason ?? "<none>"}");
        logMessageBuilder.Append($"Expires: {expirationTime?.ToString() ?? "never"}");
        Logger.Info(logMessageBuilder);

        if (type != InfractionType.Gag && options.NotifyUser && guild.Members.TryGetValue(user.Id, out DiscordMember? member))
        {
            DiscordEmbed embed = await CreatePrivateInfractionEmbedAsync(infraction);
            await member.SendMessageAsync(embed);
        }

        return infraction;
    }

    /// <summary>
    ///     Creates an infraction embed to send to the staff log channel.
    /// </summary>
    /// <param name="infraction">The infraction to log.</param>
    public async Task<DiscordEmbed> CreateInfractionEmbedAsync(Infraction infraction)
    {
        DiscordGuild? guild = await _discordClient.GetGuildAsync(infraction.GuildId);
        DiscordUser? user = await _discordClient.GetUserAsync(infraction.UserId);
        DiscordUser? staffMember = await _discordClient.GetUserAsync(infraction.StaffMemberId);
        int infractionCount = GetInfractionCount(user, guild);

        string reason = string.IsNullOrWhiteSpace(infraction.Reason)
            ? Formatter.Italic("<none>")
            : infraction.Reason;

        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed(false).WithColor(0xFF0000);
        embedBuilder.AddField(Formatter.Underline("Infraction ID"), infraction.Id, true);
        embedBuilder.AddField(Formatter.Underline("User"), user, true);
        embedBuilder.AddField(Formatter.Underline("Staff Member"), staffMember, true);
        embedBuilder.AddField(Formatter.Underline("Total User Infractions"), infractionCount, true);
        embedBuilder.AddField(Formatter.Underline("Type"), infraction.Type.Humanize(), true);
        embedBuilder.AddField(Formatter.Underline("Time"),
            Formatter.Timestamp(infraction.IssuedAt, TimestampFormat.ShortDateTime),
            true);
        embedBuilder.AddField(Formatter.Underline("Reason"), reason);

        return embedBuilder.Build();
    }

    public async Task<DiscordEmbedBuilder> BuildInfractionHistoryEmbedAsync(DiscordUser user, DiscordGuild guild,
        bool staffRequested, int page = 0)
    {
        const int infractionsPerPage = 10;

        IReadOnlyList<IInfraction> infractions = GetInfractions(user, guild);
        GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed();
        embed.WithAuthor(user);
        embed.WithColor(guildConfiguration.PrimaryColor);

        string underlinedFieldName = Formatter.Underline("Infraction Record");
        if (infractions.Count > 0)
        {
            var infractionList = new List<string>();
            var id = 0L;

            foreach (IInfraction infraction in infractions.Skip(page * infractionsPerPage).Take(infractionsPerPage))
            {
                if (staffRequested) id = infraction.Id;
                else id++;

                infractionList.Add(await BuildInfractionStringAsync(infraction, id));
            }

            embed.AddField(underlinedFieldName, string.Join("\n\n", infractionList));
        }
        else
            embed.AddField(underlinedFieldName, "✅ No infractions on record");

        return embed;

        async Task<string> BuildInfractionStringAsync(IInfraction infraction, long id)
        {
            var builder = new StringBuilder();

            builder.Append(Formatter.Bold($"ID: {id}")).Append(" \u2022 ");
            builder.AppendLine($"Issued at {Formatter.Timestamp(infraction.IssuedAt, TimestampFormat.ShortDate)}");
            builder.Append($"Punishment: {infraction.Type.Humanize()}");

            if (staffRequested)
            {
                DiscordMember staffMember = await guild.GetMemberAsync(infraction.StaffMemberId);
                builder.Append($" by {staffMember.Mention}");
            }

            builder.AppendLine().AppendLine(infraction.Reason);

            return builder.ToString().Trim();
        }
    }

    /// <summary>
    ///     Creates an infraction embed intended to be sent to the user who received the infraction.
    /// </summary>
    /// <param name="infraction">The infraction containing the details to display.</param>
    /// <returns>A <see cref="DiscordEmbed" />.</returns>
    /// <exception cref="ArgumentException">
    ///     The <see cref="Infraction.Type" /> of <paramref name="infraction" /> is <see cref="InfractionType.Gag" />.
    /// </exception>
    public async Task<DiscordEmbed> CreatePrivateInfractionEmbedAsync(Infraction infraction)
    {
        if (infraction.Type == InfractionType.Gag)
            throw new ArgumentException(ExceptionMessages.NoEmbedForGag, nameof(infraction));

        DiscordGuild? guild = await _discordClient.GetGuildAsync(infraction.GuildId);
        DiscordUser? user = await _discordClient.GetUserAsync(infraction.UserId);
        int infractionCount = GetInfractionCount(user, guild);

        string? description = infraction.Type switch
        {
            InfractionType.Warning => EmbedMessages.WarningDescription,
            InfractionType.TemporaryMute => EmbedMessages.TemporaryMuteDescription,
            InfractionType.Mute => EmbedMessages.MuteDescription,
            InfractionType.Kick => EmbedMessages.KickDescription,
            InfractionType.Ban => EmbedMessages.BanDescription,
            InfractionType.TemporaryBan => EmbedMessages.TemporaryBanDescription,
            _ => null
        };

        string reason = infraction.Reason.WithWhiteSpaceAlternative(Formatter.Italic("<no reason specified>"));

        return new DiscordEmbedBuilder()
            .WithColor(0xFF0000)
            .WithTitle(infraction.Type.Humanize())
            .WithDescription(string.IsNullOrWhiteSpace(description) ? null : description.FormatSmart(new {user, guild}))
            .WithThumbnail(guild.IconUrl)
            .WithFooter(guild.Name, guild.IconUrl)
            .AddField(EmbedFieldNames.Reason, reason)
            .AddField(EmbedFieldNames.TotalInfractions, infractionCount);
    }

    /// <summary>
    ///     Issues a ban against a user, optionally with a specified reason and duration.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the ban, or <see langword="null" /> to not specify a reason.</param>
    /// <param name="duration">The duration of the temporary ban, or <see langword="null" /> if this ban is permanent.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="duration" /> refers to a negative duration.</exception>
    public async Task<Infraction> BanAsync(DiscordUser user, DiscordMember staffMember, string? reason, TimeSpan? duration)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        if (duration.HasValue && duration.Value < TimeSpan.Zero)
            throw new ArgumentException(ExceptionMessages.NoNegativeDuration, nameof(duration));

        var options = new InfractionOptions
        {
            Duration = duration,
            Reason = reason.AsNullIfWhiteSpace()
        };

        DiscordGuild guild = staffMember.Guild;
        Infraction infraction = await CreateInfractionAsync(InfractionType.Kick, user, staffMember, options);
        await guild.BanMemberAsync(user.Id, reason: reason);
        await LogInfractionAsync(guild, infraction);
        return infraction;
    }

    /// <inheritdoc cref="IHammerPlugin.EnumerateInfractions(DiscordGuild)" />
    public IEnumerable<IInfraction> EnumerateInfractions(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        if (!_infractionCache.TryGetValue(guild, out List<Infraction>? cache))
            yield break;

        foreach (Infraction infraction in cache)
            yield return infraction;
    }

    /// <inheritdoc cref="IHammerPlugin.EnumerateInfractions(DiscordUser, DiscordGuild)" />
    public IEnumerable<IInfraction> EnumerateInfractions(DiscordUser user, DiscordGuild guild)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        if (!_infractionCache.TryGetValue(guild, out List<Infraction>? cache))
            yield break;

        foreach (Infraction infraction in cache.Where(i => i.UserId == user.Id))
            yield return infraction;
    }

    /// <inheritdoc cref="IHammerPlugin.GetInfractionCount(DiscordGuild)" />
    public int GetInfractionCount(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        return _infractionCache.TryGetValue(guild, out List<Infraction>? cache)
            ? cache.Count
            : 0;
    }

    /// <inheritdoc cref="IHammerPlugin.GetInfractionCount(DiscordUser, DiscordGuild)" />
    public int GetInfractionCount(DiscordUser user, DiscordGuild guild)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        return _infractionCache.TryGetValue(guild, out List<Infraction>? cache)
            ? cache.Count(i => i.UserId == user.Id)
            : 0;
    }

    /// <inheritdoc cref="IHammerPlugin.GetInfractions(DiscordGuild)" />
    public IReadOnlyList<IInfraction> GetInfractions(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        return _infractionCache.TryGetValue(guild, out List<Infraction>? cache)
            ? cache.ToArray()
            : ArraySegment<IInfraction>.Empty;
    }

    /// <inheritdoc cref="IHammerPlugin.GetInfractions(DiscordUser, DiscordGuild)" />
    public IReadOnlyList<IInfraction> GetInfractions(DiscordUser user, DiscordGuild guild)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        return _infractionCache.TryGetValue(guild, out List<Infraction>? cache)
            ? cache.Where(i => i.UserId == user.Id).ToArray()
            : ArraySegment<IInfraction>.Empty;
    }

    /// <summary>
    ///     Issues a gag infraction to a user.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="staffMember">The staff member responsible for the warning.</param>
    /// <returns>The newly-created infraction.</returns>
    public async Task<Infraction> GagAsync(DiscordUser user, DiscordMember staffMember)
    {
        return await CreateInfractionAsync(InfractionType.Gag, user, staffMember, default);
    }

    /// <summary>
    ///     Issues a mute against a user, optionally with a specified reason and duration.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="staffMember">The staff member responsible for the infraction.</param>
    /// <param name="reason">The reason for the mute, or <see langword="null" /> to not specify a reason.</param>
    /// <param name="duration">The duration of the temporary mute, or <see langword="null" /> if this mute is permanent.</param>
    /// <returns>The newly-created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="duration" /> refers to a negative duration.</exception>
    public async Task<Infraction> MuteAsync(DiscordUser user, DiscordMember staffMember, string? reason, TimeSpan? duration)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        if (duration.HasValue && duration.Value < TimeSpan.Zero)
            throw new ArgumentException(ExceptionMessages.NoNegativeDuration, nameof(duration));

        var options = new InfractionOptions
        {
            Duration = duration,
            Reason = reason.AsNullIfWhiteSpace()
        };

        DiscordGuild guild = staffMember.Guild;
        Infraction infraction = await CreateInfractionAsync(InfractionType.Kick, user, staffMember, options);
        await LogInfractionAsync(guild, infraction);

        DiscordMember? member = await guild.GetMemberAsync(user.Id);
        if (member is not null)
        {
            DiscordRole? mutedRole = _temporaryMuteService.GetMutedRole(guild);
            if (mutedRole is null)
                Logger.Warn(LoggerMessages.NoMutedRoleToGrant.FormatSmart(new {guild}));
            else
            {
                Logger.Info(LoggerMessages.MemberMuted.FormatSmart(new {user, staffMember, guild}));
                await member.GrantRoleAsync(mutedRole);
            }
        }
        else
            Logger.Warn(LoggerMessages.CantMuteNonMember.FormatSmart(new {user, guild}));

        if (duration.HasValue)
            _temporaryMuteService.CreateTemporaryMute(user, guild, duration.Value);

        return infraction;
    }

    /// <summary>
    ///     Kicks a member from the guild.
    /// </summary>
    /// <param name="member">The member to kick.</param>
    /// <param name="staffMember">The staff member responsible for the kick.</param>
    /// <param name="reason">The reason for the kick.</param>
    /// <returns>The newly-created infraction resulting from the kick.</returns>
    /// <exception cref="ArgumentException">
    ///     <para><paramref name="member" /> and <paramref name="staffMember" /> are not in the same guild.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<Infraction> KickAsync(DiscordMember member, DiscordMember staffMember, string? reason)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        if (member.Guild != staffMember.Guild)
            throw new ArgumentException(ExceptionMessages.StaffMemberRecipientGuildMismatch, nameof(staffMember));

        var options = new InfractionOptions {Reason = reason.AsNullIfWhiteSpace()};
        Infraction infraction = await CreateInfractionAsync(InfractionType.Kick, member, staffMember, options);
        await member.RemoveAsync(reason);

        await LogInfractionAsync(staffMember.Guild, infraction);
        return infraction;
    }

    /// <summary>
    ///     Logs an infraction to the staff log channel.
    /// </summary>
    /// <param name="guild">The guild in which to log.</param>
    /// <param name="infraction">The infraction to log.</param>
    /// <param name="notificationOptions">
    ///     Optional. The staff notification options. Defaults to <see cref="StaffNotificationOptions.None" />.
    /// </param>
    public async Task LogInfractionAsync(DiscordGuild guild, Infraction infraction,
        StaffNotificationOptions notificationOptions = StaffNotificationOptions.None)
    {
        DiscordEmbed embed = await CreateInfractionEmbedAsync(infraction);
        await _corePlugin.LogAsync(guild, embed, notificationOptions);
    }

    public async Task UnmuteAsync(DiscordUser user, DiscordMember staffMember)
    {
        DiscordGuild guild = staffMember.Guild;

        await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
        {
            await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
            Infraction? infraction = await context.Infractions.OrderByDescending(i => i.Id)
                .FirstOrDefaultAsync(i =>
                    (i.Type == InfractionType.Mute || i.Type == InfractionType.TemporaryMute) &&
                    i.UserId == user.Id &&
                    i.GuildId == guild.Id);

            if (infraction is null)
            {
                // display error
            }
            else
            {
                infraction.ExpirationTime = DateTimeOffset.UtcNow;
                if (guild.Members.TryGetValue(user.Id, out DiscordMember? member))
                {
                    DiscordRole? mutedRole = _temporaryMuteService.GetMutedRole(guild);
                    if (mutedRole is null)
                        Logger.Warn(LoggerMessages.NoMutedRoleToGrant.FormatSmart(new {guild}));
                    else
                    {
                        Logger.Info(LoggerMessages.MemberMuted.FormatSmart(new {user, staffMember, guild}));
                        await member.RevokeRoleAsync(mutedRole);
                    }
                }
                else
                    Logger.Warn(LoggerMessages.CantMuteNonMember.FormatSmart(new {user, guild}));
            }
        }

        _temporaryMuteService.ClearTemporaryMute(user, guild);
    }

    /// <summary>
    ///     Issues a warning against a user.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="staffMember">The staff member responsible for the warning.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <returns>The newly-created infraction resulting from the warning.</returns>
    public async Task<Infraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string? reason)
    {
        reason = reason.AsNullIfWhiteSpace();

        var options = new InfractionOptions {Reason = reason};
        Infraction infraction = await CreateInfractionAsync(InfractionType.Warning, user, staffMember, options);

        await LogInfractionAsync(staffMember.Guild, infraction);
        return infraction;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private async Task LoadGuildInfractions(DiscordGuild guild)
    {
        if (!_infractionCache.TryGetValue(guild, out List<Infraction>? cache))
        {
            cache = new List<Infraction>();
            _infractionCache.Add(guild, cache);
        }

        cache.Clear();

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        foreach (Infraction infraction in context.Infractions.Where(i => i.GuildId == guild.Id))
        {
            if (!cache.Exists(i => i.Id == infraction.Id))
                cache.Add(infraction);
        }
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return LoadGuildInfractions(e.Guild);
    }
}
