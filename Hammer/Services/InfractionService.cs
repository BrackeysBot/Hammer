using System.Collections.Concurrent;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using X10D.Text;
using TimestampFormat = DSharpPlus.TimestampFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles and manipulates infractions.
/// </summary>
/// <seealso cref="BanService" />
/// <seealso cref="MuteService" />
internal sealed class InfractionService : BackgroundService
{
    private readonly ConcurrentDictionary<ulong, List<Infraction>> _infractionCache = new();
    private readonly ILogger<InfractionService> _logger;
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly DiscordClient _discordClient;
    private readonly AltAccountService _altAccountService;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly MailmanService _mailmanService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionService" /> class.
    /// </summary>
    public InfractionService(
        ILogger<InfractionService> logger,
        IDbContextFactory<HammerContext> dbContextFactory,
        DiscordClient discordClient,
        AltAccountService altAccountService,
        ConfigurationService configurationService,
        DiscordLogService logService,
        InfractionCooldownService cooldownService,
        MailmanService mailmanService,
        RuleService ruleService
    )
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _discordClient = discordClient;
        _altAccountService = altAccountService;
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
    public Infraction AddInfraction(Infraction infraction, DiscordGuild? guild = null)
    {
        if (guild is null && !_discordClient.Guilds.TryGetValue(infraction.GuildId, out guild))
            throw new InvalidOperationException("The specified guild is invalid.");

        using HammerContext context = _dbContextFactory.CreateDbContext();

        try
        {
            infraction = context.Add(infraction).Entity;
            context.SaveChanges();
        }
        catch (DbUpdateException exception)
        {
            // SQLite error 19 is "constraint" - this is almost certainly due to duplicate unique key,
            // which may happen with an attempt to migrate an infraction with an ID already used.
            // in this case, we can set ID to 0 so that Sqlite generates a new sequential ID.
            // if the error is not 19, or if THIS operation fails, just rethrow because it's not our concern here.

            if (exception.InnerException is not SqliteException {SqliteErrorCode: 19})
                throw;

            infraction.Id = 0;
            infraction = context.Add(infraction).Entity;
            context.SaveChanges();
        }

        List<Infraction> infractions = _infractionCache.AddOrUpdate(guild.Id, _ => new List<Infraction>(), (_, list) => list);
        infractions.Add(infraction);
        return infraction;
    }

    /// <summary>
    ///     Adds infractions to the database.
    /// </summary>
    /// <param name="infractions">The infractions to add.</param>
    /// <remarks>Do NOT use this method to issue infractions to users. Use an appropriate user-targeted method.</remarks>
    public void AddInfractions(IEnumerable<Infraction> infractions)
    {
        infractions = infractions.ToArray();

        foreach (IGrouping<ulong, Infraction> group in infractions.GroupBy(i => i.GuildId))
        {
            ulong guildId = group.Key;
            List<Infraction> cache = _infractionCache.AddOrUpdate(guildId, _ => new List<Infraction>(), (_, list) => list);
            cache.AddRange(group);
        }

        using HammerContext context = _dbContextFactory.CreateDbContext();
        context.AddRange(infractions);
        context.SaveChangesAsync();
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

        if (type == InfractionType.Gag)
            expirationTime = DateTimeOffset.UtcNow + options.Duration;

        var builder = new InfractionBuilder();
        builder.WithType(type);
        builder.WithTargetUser(user).WithGuild(guild);
        builder.WithReason(reason).WithStaffMember(staffMember);
        builder.WithRule(options.RuleBroken);

        if (expirationTime.HasValue)
            builder.WithAdditionalInformation($"Duration: {(DateTimeOffset.UtcNow - expirationTime.Value).Humanize()}");

        Infraction infraction = AddInfraction(builder.Build(), guild);

        var logMessageBuilder = new StringBuilder();
        logMessageBuilder.Append($"{type.ToString("G")} issued to {user} by {staffMember} in {guild}. ");
        logMessageBuilder.Append($"Reason: {reason ?? "<none>"}. ");
        logMessageBuilder.Append($"Rule broken: {options.RuleBroken?.Id.ToString() ?? "<none>"}. ");
        logMessageBuilder.Append($"Expires: {expirationTime?.ToString() ?? "never"}");
        _logger.LogInformation("{Message}", logMessageBuilder.ToString());

        if (type != InfractionType.Gag && options.NotifyUser)
        {
            int count = GetInfractionCount(user, staffMember.Guild);
            DiscordMessage? dm = await _mailmanService.SendInfractionAsync(infraction, count, options).ConfigureAwait(false);
            result &= dm is not null;
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
        if (infraction.RuleId is { } ruleId && _ruleService.GuildHasRule(infraction.GuildId, ruleId))
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        embedBuilder.WithTitle(infraction.Type.Humanize());
        embedBuilder.AddField("Infraction ID", infraction.Id, true);
        embedBuilder.AddField("User", MentionUtility.MentionUser(infraction.UserId), true);
        embedBuilder.AddField("User ID", infraction.UserId.ToString(), true);
        embedBuilder.AddField("Staff Member", MentionUtility.MentionUser(infraction.StaffMemberId), true);
        embedBuilder.AddField("Reason", reason);
        embedBuilder.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embedBuilder.AddField("Total User Infractions", infractionCount, true);
        embedBuilder.AddFieldIf(!string.IsNullOrWhiteSpace(infraction.AdditionalInformation),
            "Additional Information",
            () => infraction.AdditionalInformation);

        return embedBuilder.Build();
    }

    /// <summary>
    ///     Builds an infraction history embed.
    /// </summary>
    /// <param name="response">The infraction history response.</param>
    /// <param name="page">The zero-based page index of infractions to create.</param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>A new instance of <see cref="DiscordEmbedBuilder" /> containing the infraction history.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="searchOptions" /> contains invalid property values.</exception>
    public DiscordEmbedBuilder BuildInfractionHistoryEmbed(
        InfractionHistoryResponse response,
        int page,
        InfractionSearchOptions searchOptions = default
    )
    {
        ArgumentNullException.ThrowIfNull(response);

        switch (searchOptions)
        {
            case {IssuedAfter: { } afterDate, IssuedBefore: { } beforeDate} when afterDate > beforeDate:
                throw new ArgumentException(ExceptionMessages.MinDateGreaterThanMaxDate, nameof(searchOptions));
            case {IdAfter: { } afterId, IdBefore: { } beforeId} when afterId > beforeId:
                throw new ArgumentException(ExceptionMessages.MinIdGreaterThanMaxId, nameof(searchOptions));
        }

        DiscordUser user = response.TargetUser;
        IReadOnlyList<Infraction> infractions = GetInfractions(user, response.Guild, searchOptions);
        bool hasSearchQuery = !searchOptions.IsEmpty;

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithAuthor(user);
        IReadOnlyCollection<ulong> alts = _altAccountService.GetAltsFor(user.Id);
        Infraction[] altInfractions = alts.SelectMany(alt => GetInfractions(alt, response.Guild.Id, searchOptions)).ToArray();
        
        if (response.StaffRequested && page == response.Pages - 1 && alts.Count > 0 && altInfractions.Length > 0)
        {
            string infractionNumber = "additional infraction".ToQuantity(altInfractions.Length);
            string altNumber = "alt account".ToQuantity(alts.Count);
            embed.WithFooter($"⚠️ This user has {infractionNumber} on {altNumber}.");
        }

        const int infractionsPerPage = 10;
        page = (int) Math.Clamp(page, 0, MathF.Ceiling(infractions.Count / 10.0f));

        if (infractions.Count > 0)
        {
            if (page == 0)
            {
                embed.WithTitle(hasSearchQuery
                    ? $"__{"result".ToQuantity(infractions.Count)}__"
                    : $"__{"infraction".ToQuantity(infractions.Count)} on record__");
            }

            embed.WithDescription(
                string.Join('\n',
                    infractions.OrderByDescending(i => i.IssuedAt)
                        .Skip(infractionsPerPage * page)
                        .Take(infractionsPerPage)
                        .Select(BuildInfractionString)));
        }
        else
        {
            embed.WithDescription($"**✅ {(hasSearchQuery ? "Result returned no matches" : "No infractions on record")}**");
        }

        return embed;

        string BuildInfractionString(Infraction infraction, int index)
        {
            var builder = new StringBuilder();
            var content = $"ID: {(response.StaffRequested ? infraction.Id : index + 1 + page * infractionsPerPage)}";

            builder.Append(Formatter.Bold(content)).Append(" \u2022 ");
            builder.Append(infraction.Type.Humanize()).Append(" \u2022 ");
            if (infraction.Reason is { } reason) builder.Append(reason);
            else builder.Append("<none>");

            builder.Append(" \u2022 ");
            builder.Append(Formatter.Timestamp(infraction.IssuedAt));

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
        return await CreateInfractionAsync(InfractionType.Gag, user, staffMember, new InfractionOptions {NotifyUser = false, Duration = duration.Value, ExpirationTime = gagUntil})
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
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>The count of infractions for <paramref name="user" /> in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public int GetInfractionCount(DiscordUser user, DiscordGuild guild, InfractionSearchOptions searchOptions = default)
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
            Infraction infraction = cache[index];

            if (searchOptions.Type is { } type && infraction.Type != type) continue;
            if (searchOptions.IssuedAfter is { } minimum && infraction.IssuedAt < minimum) continue;
            if (searchOptions.IssuedBefore is { } maximum && infraction.IssuedAt > maximum) continue;

            if (infraction.UserId == userId)
                total++;
        }

        return total;
    }

    /// <summary>
    ///     Gets the total infraction count for a user in the specified guild.
    /// </summary>
    /// <param name="userId">The ID of the user whose infractions to count.</param>
    /// <param name="guildId">The ID of the guild whose infractions to search.</param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>The count of infractions for <paramref name="userId" /> in <paramref name="guildId" />.</returns>
    public int GetInfractionCount(ulong userId, ulong guildId, InfractionSearchOptions searchOptions = default)
    {
        if (!_infractionCache.TryGetValue(guildId, out List<Infraction>? cache))
            return 0;

        var total = 0;
        int count = cache.Count;

        for (var index = 0; index < count; index++)
        {
            Infraction infraction = cache[index];

            if (searchOptions.Type is { } type && infraction.Type != type) continue;
            if (searchOptions.IssuedAfter is { } minimum && infraction.IssuedAt < minimum) continue;
            if (searchOptions.IssuedBefore is { } maximum && infraction.IssuedAt > maximum) continue;

            if (infraction.UserId == userId)
                total++;
        }

        return total;
    }

    /// <summary>
    ///     Returns all infractions for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to return.</param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>A read-only view of the list of <see cref="Infraction" /> objects held for <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="searchOptions" /> contains invalid property values.</exception>
    public IReadOnlyList<Infraction> GetInfractions(DiscordGuild guild, InfractionSearchOptions searchOptions = default)
    {
        ArgumentNullException.ThrowIfNull(guild);

        switch (searchOptions)
        {
            case {IssuedAfter: { } afterDate, IssuedBefore: { } beforeDate} when afterDate > beforeDate:
                throw new ArgumentException(ExceptionMessages.MinDateGreaterThanMaxDate, nameof(searchOptions));
            case {IdAfter: { } afterId, IdBefore: { } beforeId} when afterId > beforeId:
                throw new ArgumentException(ExceptionMessages.MinIdGreaterThanMaxId, nameof(searchOptions));
        }

        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            return ArraySegment<Infraction>.Empty;

        var infractions = new Infraction[cache.Count];
        var resultIndex = 0;

        for (var index = 0; index < infractions.Length; index++)
        {
            Infraction infraction = cache[index];

            if (searchOptions.Type is { } type && infraction.Type != type) continue;
            if (searchOptions.IssuedAfter is { } minimum && infraction.IssuedAt < minimum) continue;
            if (searchOptions.IssuedBefore is { } maximum && infraction.IssuedAt > maximum) continue;

            infractions[resultIndex++] = infraction;
        }

        return new ArraySegment<Infraction>(infractions, 0, resultIndex);
    }

    /// <summary>
    ///     Returns all infractions for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to return.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>
    ///     A read-only view of the list of <see cref="Infraction" /> objects issued to <paramref name="user" /> in
    ///     <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="searchOptions" /> contains invalid property values.</exception>
    public IReadOnlyList<Infraction> GetInfractions(
        DiscordUser user,
        DiscordGuild guild,
        InfractionSearchOptions searchOptions = default
    )
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        return GetInfractions(user.Id, guild.Id, searchOptions);
    }

    /// <summary>
    ///     Returns all infractions for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose infractions to return.</param>
    /// <param name="guild">The guild whose infractions to search.</param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <returns>
    ///     A read-only view of the list of <see cref="Infraction" /> objects issued to <paramref name="user" /> in
    ///     <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" /></para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="searchOptions" /> contains invalid property values.</exception>
    public IReadOnlyList<Infraction> GetInfractions(
        ulong userId,
        ulong guildId,
        InfractionSearchOptions searchOptions = default
    )
    {
        switch (searchOptions)
        {
            case {IssuedAfter: { } afterDate, IssuedBefore: { } beforeDate} when afterDate > beforeDate:
                throw new ArgumentException(ExceptionMessages.MinDateGreaterThanMaxDate, nameof(searchOptions));
            case {IdAfter: { } afterId, IdBefore: { } beforeId} when afterId > beforeId:
                throw new ArgumentException(ExceptionMessages.MinIdGreaterThanMaxId, nameof(searchOptions));
        }

        if (!_infractionCache.TryGetValue(guildId, out List<Infraction>? cache))
            return ArraySegment<Infraction>.Empty;

        int count = cache.Count;
        var infractions = new Infraction[count];
        var resultIndex = 0;

        for (var index = 0; index < infractions.Length; index++)
        {
            Infraction infraction = cache[index];

            if (infraction.UserId != userId) continue;
            if (searchOptions.Type is { } type && infraction.Type != type) continue;
            if (searchOptions.IssuedAfter is { } minimum && infraction.IssuedAt < minimum) continue;
            if (searchOptions.IssuedBefore is { } maximum && infraction.IssuedAt > maximum) continue;

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
    public void ModifyInfraction(Infraction infraction, Action<Infraction> action)
    {
        ArgumentNullException.ThrowIfNull(infraction);
        ArgumentNullException.ThrowIfNull(action);

        using HammerContext context = _dbContextFactory.CreateDbContext();
        Infraction? existing = context.Infractions.Find(infraction.Id);
        if (existing is null) return;

        action(existing);
        context.Update(existing);
        context.SaveChanges();

        if (_infractionCache.TryGetValue(infraction.GuildId, out List<Infraction>? cache))
        {
            cache.Remove(infraction);
            cache.Remove(existing);
            cache.Add(existing);
        }
    }

    /// <summary>
    ///     Prunes all stale infractions from the database.
    /// </summary>
    public async Task<int> PruneStaleInfractionsAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var idCache = new Dictionary<ulong, bool>();
        var pruneInfractions = new List<Infraction>();

        await foreach (Infraction infraction in context.Infractions)
        {
            ulong userId = infraction.UserId;

            if (!idCache.TryGetValue(userId, out bool isStale))
            {
                try
                {
                    DiscordUser user = await _discordClient.GetUserAsync(userId).ConfigureAwait(false);
                    isStale = user is null;
                }
                catch (NotFoundException)
                {
                    isStale = true;
                }

                idCache[userId] = isStale;
            }

            if (isStale)
                pruneInfractions.Add(infraction);
        }

        foreach (Infraction infraction in pruneInfractions)
        {
            if (_infractionCache.TryGetValue(infraction.GuildId, out List<Infraction>? cache))
                cache.Remove(infraction);
            context.Remove(infraction);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return pruneInfractions.Count;
    }

    /// <summary>
    ///     Redacts an infraction.
    /// </summary>
    /// <param name="infraction">The infraction to redact.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infraction" /> is <see langword="null" />.</exception>
    public void RemoveInfraction(Infraction infraction)
    {
        ArgumentNullException.ThrowIfNull(infraction);

        _cooldownService.StopCooldown(infraction.UserId);
        _infractionCache[infraction.GuildId].Remove(infraction);

        using HammerContext context = _dbContextFactory.CreateDbContext();
        context.Remove(infraction);
        context.SaveChanges();
    }

    /// <summary>
    ///     Redacts a collection of infractions.
    /// </summary>
    /// <param name="infractions">The infractions to redact.</param>
    /// <exception cref="ArgumentNullException"><paramref name="infractions" /> is <see langword="null" />.</exception>
    public void RemoveInfractions(IEnumerable<Infraction> infractions)
    {
        ArgumentNullException.ThrowIfNull(infractions);

        using HammerContext context = _dbContextFactory.CreateDbContext();

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

        context.SaveChanges();
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.GuildAvailable -= OnGuildAvailable;
        _discordClient.GuildUnavailable -= OnGuildUnavailable;
        return base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += OnGuildAvailable;
        _discordClient.GuildUnavailable += OnGuildUnavailable;
        return Task.CompletedTask;
    }

    private void LoadGuildInfractions(DiscordGuild guild)
    {
        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
        {
            cache = new List<Infraction>();
            _infractionCache.AddOrUpdate(guild.Id, cache, (_, _) => cache);
        }

        cache.Clear();

        using HammerContext context = _dbContextFactory.CreateDbContext();
        cache.AddRange(context.Infractions.Where(i => i.GuildId == guild.Id));

        _logger.LogInformation("Retrieved {Count} infractions for {Guild}", cache.Count, guild);
    }

    private void UpdateInfractionRules(DiscordGuild guild)
    {
        if (!_infractionCache.TryGetValue(guild.Id, out List<Infraction>? cache))
            return;

        var updated = new List<Infraction>();
        foreach (Infraction infraction in cache)
        {
            if (!infraction.RuleId.HasValue || !string.IsNullOrWhiteSpace(infraction.RuleText)) continue;
            if (!_ruleService.GuildHasRule(infraction.GuildId, infraction.RuleId.Value)) continue;

            Rule rule = _ruleService.GetRuleById(infraction.GuildId, infraction.RuleId.Value);
            infraction.RuleText = rule.Brief ?? rule.Description;
            updated.Add(infraction);
        }

        if (updated.Count > 0)
        {
            _logger.LogInformation("Updating {Count} infraction rules for {Guild}", updated.Count, guild);
            
            using HammerContext context = _dbContextFactory.CreateDbContext();
            context.UpdateRange(updated);
            context.SaveChanges();
        }
    }

    private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        LoadGuildInfractions(e.Guild);
        UpdateInfractionRules(e.Guild);
        return Task.CompletedTask;
    }

    private Task OnGuildUnavailable(DiscordClient sender, GuildDeleteEventArgs args)
    {
        if (_infractionCache.TryRemove(args.Guild.Id, out List<Infraction>? infractions))
            infractions.Clear();

        return Task.CompletedTask;
    }
}
