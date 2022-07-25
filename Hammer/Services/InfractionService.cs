using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using X10D.DSharpPlus;
using X10D.Text;
using ILogger = NLog.ILogger;
using TimestampFormat = DSharpPlus.TimestampFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles and manipulates infractions.
/// </summary>
/// <seealso cref="BanService" />
/// <seealso cref="MuteService" />
internal sealed class InfractionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<ulong, List<Infraction>> _infractionCache = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly MailmanService _mailmanService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionService" /> class.
    /// </summary>
    public InfractionService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService,
        InfractionCooldownService cooldownService,
        MailmanService mailmanService,
        RuleService ruleService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
        _cooldownService = cooldownService;
        _mailmanService = mailmanService;
        _ruleService = ruleService;
    }

    /// <summary>
    ///     Adds an infraction to the database.
    /// </summary>
    /// <param name="infraction">The infraction to add.</param>
    /// <param name="guild">
    ///     The guild to which this infraction belongs. If <see langword="null" /> is passed, this method will attempt to retrieve
    ///     the guild specified by the infraction from the client.
    /// </param>
    /// <returns>The infraction entity.</returns>
    /// <remarks>
    ///     Do NOT use this method to issue infractions to users. Use an appropriate user-targeted method from another service.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The infraction belongs to a guild that this client cannot access.</exception>
    /// <seealso cref="CreateInfractionAsync" />
    public async Task<Infraction> AddInfractionAsync(Infraction infraction, DiscordGuild? guild = null)
    {
        try
        {
            guild ??= await _discordClient.GetGuildAsync(infraction.GuildId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            throw new InvalidOperationException("The specified guild is invalid.");
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        infraction = (await context.AddAsync(infraction).ConfigureAwait(false)).Entity;
        await context.SaveChangesAsync();

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? infractions))
        {
            infractions = new List<Infraction>();
            _infractionCache.Add(guild.Id, infractions);
        }

        infractions.Add(infraction);
        return infraction;
    }

    /// <summary>
    ///     Adds infractions to the database.
    /// </summary>
    /// <param name="infractions">The infractions to add.</param>
    /// <remarks>Do NOT use this method to issue infractions to users. Use an appropriate user-targeted method.</remarks>
    public async Task AddInfractionsAsync(IEnumerable<Infraction> infractions)
    {
        infractions = infractions.ToArray();

        foreach (Infraction infraction in infractions)
        {
            if (!_infractionCache.TryGetValue(infraction.GuildId, out List<Infraction>? cache))
            {
                cache = new List<Infraction>();
                _infractionCache.Add(infraction.GuildId, cache);
            }

            cache.Add(infraction);
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.AddRangeAsync(infractions).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
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
    /// <returns>
    ///     A tuple containing the newly-created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    public async Task<(Infraction Infraction, bool DmSuccess)> CreateInfractionAsync(
        InfractionType type,
        DiscordUser user,
        DiscordMember staffMember,
        InfractionOptions options
    )
    {
        string? reason = options.Reason.AsNullIfWhiteSpace();
        var result = true;

        DiscordGuild guild = staffMember.Guild;
        DateTimeOffset? expirationTime = options.ExpirationTime;

        if (type == InfractionType.Gag &&
            _configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            expirationTime = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(guildConfiguration.Mute.GagDuration);

        var builder = new InfractionBuilder();
        builder.WithType(type);
        builder.WithTargetUser(user).WithGuild(guild);
        builder.WithReason(reason).WithStaffMember(staffMember);
        builder.WithRule(options.RuleBroken);

        Infraction infraction = await AddInfractionAsync(builder.Build(), guild).ConfigureAwait(false);

        var logMessageBuilder = new StringBuilder();
        logMessageBuilder.Append($"{type.ToString("G")} issued to {user} by {staffMember} in {guild}. ");
        logMessageBuilder.Append($"Reason: {reason ?? "<none>"}. ");
        logMessageBuilder.Append($"Rule broken: {options.RuleBroken?.Id.ToString() ?? "<none>"}. ");
        logMessageBuilder.Append($"Expires: {expirationTime?.ToString() ?? "never"}");
        Logger.Info(logMessageBuilder);

        if (type != InfractionType.Gag && options.NotifyUser)
        {
            int infractionCount = GetInfractionCount(user, staffMember.Guild);
            DiscordMessage? dm = await _mailmanService.SendInfractionAsync(infraction, infractionCount).ConfigureAwait(false);
            if (dm is null) result = false;
        }

        _cooldownService.StartCooldown(infraction);
        return (infraction, result);
    }

    /// <summary>
    ///     Creates an infraction embed to send to the staff log channel.
    /// </summary>
    /// <param name="infraction">The infraction to log.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public async Task<DiscordEmbed> CreateInfractionEmbedAsync(Infraction infraction)
    {
        ArgumentNullException.ThrowIfNull(infraction);

        DiscordUser? user;
        try
        {
            user = await _discordClient.GetUserAsync(infraction.UserId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            user = null;
        }

        int infractionCount = GetInfractionCount(infraction.UserId, infraction.GuildId);

        string reason = string.IsNullOrWhiteSpace(infraction.Reason)
            ? Formatter.Italic("<none>")
            : infraction.Reason;

        var embedBuilder = new DiscordEmbedBuilder();
        embedBuilder.WithColor(0xFF0000);

        if (user is null) embedBuilder.WithAuthor($"User {infraction.UserId}");
        else embedBuilder.WithAuthor(user);

        Rule? rule = null;
        if (infraction.RuleId is { } ruleId)
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        embedBuilder.WithTitle(infraction.Type.Humanize());
        embedBuilder.AddField("Infraction ID", infraction.Id, true);
        embedBuilder.AddField("User", MentionUtility.MentionUser(infraction.UserId), true);
        embedBuilder.AddField("User ID", infraction.UserId.ToString(), true);
        embedBuilder.AddField("Staff Member", MentionUtility.MentionUser(infraction.StaffMemberId), true);
        embedBuilder.AddField("Reason", reason);
        embedBuilder.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embedBuilder.AddField("Total User Infractions", infractionCount, true);

        return embedBuilder.Build();
    }

    /// <summary>
    ///     Builds an infraction history embed.
    /// </summary>
    /// <param name="user">The user whose infractions to display.</param>
    /// <param name="guild">The guild in which this history was requested.</param>
    /// <param name="staffRequested">
    ///     <see langword="true" /> if this history was requested by a staff member; otherwise, <see langword="false" />.</param>
    /// <param name="page">The page of infractions to retrieve.</param>
    /// <returns>A new instance of <see cref="DiscordEmbedBuilder" /> containing the infraction history.</returns>
    public DiscordEmbedBuilder BuildInfractionHistoryEmbed(DiscordUser user, DiscordGuild guild, bool staffRequested,
        int page = 0)
    {
        const int infractionsPerPage = 10;

        IReadOnlyList<Infraction> infractions = GetInfractions(user, guild);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithAuthor(user);

        string underlinedFieldName = Formatter.Underline("Infraction Record");
        if (infractions.Count > 0)
        {
            IEnumerable<Infraction> infractionList = infractions.Skip(page * infractionsPerPage).Take(infractionsPerPage);
            embed.AddField(underlinedFieldName, string.Join("\n\n", infractionList.Select(BuildInfractionString)));
        }
        else
            embed.AddField(underlinedFieldName, "✅ No infractions on record");

        return embed;

        string BuildInfractionString(Infraction infraction, int index)
        {
            var builder = new StringBuilder();

            builder.Append(Formatter.Bold($"ID: {(staffRequested ? infraction.Id : index + 1)}")).Append(" \u2022 ");
            builder.AppendLine($"Issued at {Formatter.Timestamp(infraction.IssuedAt, TimestampFormat.ShortDate)}");
            builder.Append($"Punishment: {infraction.Type.Humanize()}");

            if (staffRequested)
                builder.Append($" by {MentionUtility.MentionUser(infraction.StaffMemberId)}");

            builder.AppendLine().AppendLine(infraction.Reason);

            return builder.ToString().Trim();
        }
    }

    /// <summary>
    ///     Enumerates all infractions for a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to enumerate.</param>
    /// <returns>An enumerable collection of <see cref="Infraction" /> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IEnumerable<Infraction> EnumerateInfractions(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            yield break;

        foreach (Infraction infraction in cache)
            yield return infraction;
    }

    /// <summary>
    ///     Enumerates the infractions issued to a user in the specified guild.
    /// </summary>
    /// <param name="userId">The ID of the user whose infractions to enumerate.</param>
    /// <param name="guildId">The ID of the guild whose infractions to search.</param>
    /// <returns>An enumerable collection of <see cref="Infraction" /> objects.</returns>
    public IEnumerable<Infraction> EnumerateInfractions(ulong userId, ulong guildId)
    {
        if (!_infractionCache.TryGetValue(guildId, out List<Infraction>? cache))
            yield break;

        foreach (Infraction infraction in cache.Where(i => i.UserId == userId))
            yield return infraction;
    }

    /// <summary>
    ///     Enumerates the infractions issued to a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to enumerate.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>An enumerable collection of <see cref="Infraction" /> objects.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public IEnumerable<Infraction> EnumerateInfractions(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            yield break;

        foreach (Infraction infraction in cache.Where(i => i.UserId == user.Id))
            yield return infraction;
    }

    /// <summary>
    ///     Issues a gag infraction to a user.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="staffMember">The staff member responsible for the warning.</param>
    /// <param name="sourceMessage">The message to which the staff member reacted.</param>
    /// <param name="duration">
    ///     The duration of the gag. If <see langword="null" />, the duration as specified in the configuration file is used.
    /// </param>
    /// <returns>The newly-created infraction, or <see langword="null" /> if the infraction could not be created.</returns>
    public async Task<(Infraction?, bool)> GagAsync(
        DiscordUser user,
        DiscordMember staffMember,
        DiscordMessage? sourceMessage = null,
        TimeSpan? duration = null
    )
    {
        DiscordGuild guild = staffMember.Guild;
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            return (null, false);

        if (!duration.HasValue)
        {
            long gagDurationMilliseconds = guildConfiguration.Mute.GagDuration;
            duration = TimeSpan.FromMilliseconds(gagDurationMilliseconds);
        }

        DateTimeOffset gagUntil = DateTimeOffset.UtcNow + duration.Value;

        try
        {
            DiscordMember member = await guild.GetMemberAsync(user.Id).ConfigureAwait(false);
            await member.TimeoutAsync(gagUntil, $"Gagged by {staffMember.GetUsernameWithDiscriminator()}").ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            // user is not in the guild. we can safely ignore this
        }

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithAuthor(user);
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("User Gagged");
        embed.AddField("User", user.Mention, true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        embed.AddField("Duration", duration.Value.Humanize(), true);

        if (sourceMessage is not null)
        {
            bool hasContent = !string.IsNullOrWhiteSpace(sourceMessage.Content);
            bool hasAttachments = sourceMessage.Attachments.Count > 0;

            string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(sourceMessage.Content)) : null;
            string? attachments = hasAttachments ? string.Join('\n', sourceMessage.Attachments.Select(a => a.Url)) : null;
            string messageLink = Formatter.MaskedUrl(sourceMessage.Id.ToString(), sourceMessage.JumpLink);
            string timestamp = Formatter.Timestamp(sourceMessage.CreationTimestamp, TimestampFormat.ShortDateTime);

            embed.AddField("Message ID", messageLink, true);
            embed.AddField("Message Time", timestamp, true);
            embed.AddFieldIf(hasContent, "Content", content);
            embed.AddFieldIf(hasAttachments, "Attachments", attachments);
        }

        await _logService.LogAsync(guild, embed).ConfigureAwait(false);
        return await CreateInfractionAsync(InfractionType.Gag, user, staffMember, new InfractionOptions {NotifyUser = false})
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the infraction with the specified ID.
    /// </summary>
    /// <param name="infractionId">The ID of the infraction to get.</param>
    /// <returns>The infraction with the specified ID, or <see langword="null" /> if no such infraction exists.</returns>
    public Infraction? GetInfraction(long infractionId)
    {
        return _infractionCache.Values.SelectMany(i => i).FirstOrDefault(i => i.Id == infractionId);
    }

    /// <summary>
    ///     Gets the total infraction count for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>The count of infractions in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetInfractionCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache) ? cache.Count : 0;
    }

    /// <summary>
    ///     Gets the total infraction count for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to count.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>The count of infractions for <paramref name="user" /> in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public int GetInfractionCount(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            return 0;

        var total = 0;
        int count = cache.Count;
        ulong userId = user.Id;

        for (var index = 0; index < count; index++)
        {
            if (cache[index].UserId == userId)
                total++;
        }

        return total;
    }

    /// <summary>
    ///     Gets the total infraction count for a user in the specified guild.
    /// </summary>
    /// <param name="userId">The ID of the user whose infractions to count.</param>
    /// <param name="guildId">The ID of the guild whose infractions to search.</param>
    /// <returns>The count of infractions for <paramref name="userId" /> in <paramref name="guildId" />.</returns>
    public int GetInfractionCount(ulong userId, ulong guildId)
    {
        if (!_infractionCache.TryGetValue(guildId, out List<Infraction>? cache))
            return 0;

        var total = 0;
        int count = cache.Count;

        for (var index = 0; index < count; index++)
        {
            if (cache[index].UserId == userId)
                total++;
        }

        return total;
    }

    /// <summary>
    ///     Returns all infractions for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to return.</param>
    /// <returns>A read-only view of the list of <see cref="Infraction" /> objects held for <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Infraction> GetInfractions(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            return ArraySegment<Infraction>.Empty;

        return cache.ToArray();
    }

    /// <summary>
    ///     Returns all infractions for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to return.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <returns>
    ///     A read-only view of the list of <see cref="Infraction" /> objects issued to <paramref name="user" /> in
    ///     <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public IReadOnlyList<Infraction> GetInfractions(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            return ArraySegment<Infraction>.Empty;

        int count = cache.Count;
        var infractions = new Infraction[count];
        var resultIndex = 0;
        ulong userId = user.Id;

        for (var index = 0; index < infractions.Length; index++)
        {
            Infraction infraction = cache[index];

            if (infraction.UserId == userId)
                infractions[resultIndex++] = infraction;
        }

        return new ArraySegment<Infraction>(infractions, 0, resultIndex);
    }

    /// <summary>
    ///     Logs an infraction to the staff log channel.
    /// </summary>
    /// <param name="guild">The guild in which to log.</param>
    /// <param name="infraction">The infraction to log.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     or
    ///     <para><paramref name="infraction" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task LogInfractionAsync(DiscordGuild guild, Infraction infraction)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(infraction);

        DiscordEmbed embed = await CreateInfractionEmbedAsync(infraction).ConfigureAwait(false);
        await _logService.LogAsync(guild, embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Modifies an infraction.
    /// </summary>
    /// <param name="infraction">The infraction to modify.</param>
    /// <param name="action">The delegate to invoke for the infraction.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="infraction" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="action" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task ModifyInfractionAsync(Infraction infraction, Action<Infraction> action)
    {
        ArgumentNullException.ThrowIfNull(infraction);
        ArgumentNullException.ThrowIfNull(action);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        infraction = context.Entry(infraction).Entity;
        action(infraction);
        context.Update(infraction);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Redacts an infraction.
    /// </summary>
    /// <param name="infraction">The infraction to redact.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public async Task RemoveInfractionAsync(Infraction infraction)
    {
        ArgumentNullException.ThrowIfNull(infraction);

        _cooldownService.StopCooldown(infraction.UserId);
        _infractionCache[infraction.GuildId].Remove(infraction);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        context.Remove(infraction);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Redacts a collection of infractions.
    /// </summary>
    /// <param name="infractions">The infractions to redact.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infractions" /> is <see langword="null" />.</exception>
    public async Task RemoveInfractionsAsync(IEnumerable<Infraction> infractions)
    {
        ArgumentNullException.ThrowIfNull(infractions);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        foreach (IGrouping<ulong, Infraction> group in infractions.GroupBy(i => i.GuildId))
        {
            List<Infraction> list = _infractionCache[group.Key];
            foreach (Infraction infraction in group)
            {
                _cooldownService.StopCooldown(infraction.UserId);
                context.Remove(infraction);
                list.Remove(infraction);
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private async Task LoadGuildInfractions(DiscordGuild guild)
    {
        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
        {
            cache = new List<Infraction>();
            _infractionCache.Add(guild.Id, cache);
        }

        cache.Clear();

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        cache.AddRange(context.Infractions.Where(i => i.GuildId == guild.Id));

        Logger.Info($"Retrieved {cache.Count} infractions for {guild}");
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return LoadGuildInfractions(e.Guild);
    }
}
