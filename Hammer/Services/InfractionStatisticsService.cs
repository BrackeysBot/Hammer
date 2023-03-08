using System.Globalization;
using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which provides an API for fetching infraction statistics.
/// </summary>
internal sealed class InfractionStatisticsService
{
    private readonly ConfigurationService _configurationService;
    private readonly BanService _banService;
    private readonly InfractionService _infractionService;
    private readonly MessageDeletionService _messageDeletionService;
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionStatisticsService" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="banService">The ban service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="messageDeletionService">The message deletion service.</param>
    /// <param name="muteService">The mute service.</param>
    public InfractionStatisticsService(ConfigurationService configurationService,
        BanService banService,
        InfractionService infractionService,
        MessageDeletionService messageDeletionService,
        MuteService muteService)
    {
        _configurationService = configurationService;
        _banService = banService;
        _infractionService = infractionService;
        _messageDeletionService = messageDeletionService;
        _muteService = muteService;
    }

    /// <summary>
    ///     Creates an embed which displays infraction statistics for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose statistics to render.</param>
    /// <returns>A <see cref="DiscordEmbed" /> populated with the statistics of <paramref name="guild" />'s infractions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="guild" /> is not a configured guild.</exception>
    public async Task<DiscordEmbed> CreateStatisticsEmbedAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException("Guild is not configured");

        (int totalBanCount, int tempBanCount, int permBanCount) = GetTotalBanCount(guild);
        (int totalMuteCount, int tempMuteCount, int permMuteCount) = GetTotalMuteCount(guild);

        int infractionCount = GetTotalInfractionCount(guild);
        int totalKickCount = GetTotalKickCount(guild);
        int totalGagCount = GetTotalGagCount(guild);
        int totalWarningCount = GetTotalWarningCount(guild);
        int usersBannedCount = GetDistinctBannedUsers(guild);
        int usersKickedCount = GetDistinctKickedUsers(guild);
        int usersMutedCount = GetDistinctMutedUsers(guild);
        int usersGaggedCount = GetDistinctGaggedUsers(guild);
        int usersWarnedCount = GetDistinctWarnedUsers(guild);
        int totalMessagesDeletedCount = await GetTotalDeletedMessageCountAsync(guild).ConfigureAwait(false);

        (float A, float B) banRatio = Ratio(permBanCount, tempBanCount);
        (float A, float B) muteRatio = Ratio(permMuteCount, tempMuteCount);
        var banRatioFormatted = $"{permBanCount:N0} perm / {tempBanCount} temp ({banRatio.A:N} : {banRatio.B:N})";
        var muteRatioFormatted = $"{permMuteCount:N0} perm / {tempMuteCount} temp ({muteRatio.A:N} : {muteRatio.B:N})";

        TimeSpan remainingBanTime = GetRemainingBanTime(guild);
        TimeSpan remainingMuteTime = GetRemainingMuteTime(guild);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration);
        embed.WithTitle("Infraction Statistics");

        embed.AddField("Total Infractions", $"{infractionCount:N0}", true);
        embed.AddField("Bans", $"{totalBanCount:N0} ({usersBannedCount:N0} distinct)", true);
        embed.AddField("Kicks", $"{totalKickCount:N0} ({usersKickedCount:N0} distinct)", true);
        embed.AddField("Mutes", $"{totalMuteCount:N0} ({usersMutedCount:N0} distinct)", true);
        embed.AddField("Gags", $"{totalGagCount:N0} ({usersGaggedCount:N0} distinct)", true);
        embed.AddField("Warnings", $"{totalWarningCount:N0} ({usersWarnedCount:N0} distinct)", true);
        embed.AddField("Messages Deleted", $"{totalMessagesDeletedCount:N0}", true);
        embed.AddField("Ban Ratio", banRatioFormatted, true);
        embed.AddField("Mute Ratio", muteRatioFormatted, true);
        embed.AddField("Remaining Ban Time", remainingBanTime.Humanize(culture: CultureInfo.CurrentCulture), true);
        embed.AddField("Remaining Mute Time", remainingMuteTime.Humanize(culture: CultureInfo.CurrentCulture), true);

        return embed.Build();
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received a <see cref="InfractionType.Ban" /> or a
    ///     <see cref="InfractionType.TemporaryBan" />.
    /// </summary>
    /// <param name="guild">The guild whose bans to count.</param>
    /// <returns>The total number of users who have received at least one ban in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetDistinctBannedUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> bans = _infractionService.GetInfractions(guild, InfractionType.Ban);
        IReadOnlyList<Infraction> temporaryBans = _infractionService.GetInfractions(guild, InfractionType.TemporaryBan);
        var users = new HashSet<ulong>();

        for (var index = 0; index < bans.Count; index++)
            users.Add(bans[index].UserId);

        for (var index = 0; index < temporaryBans.Count; index++)
            users.Add(temporaryBans[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received a <see cref="InfractionType.Gag" />.
    /// </summary>
    /// <param name="guild">The guild whose gags to count.</param>
    /// <returns>The total number of users who have received at least one gag in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetDistinctGaggedUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(guild, InfractionType.Gag);
        var users = new HashSet<ulong>();

        for (var index = 0; index < infractions.Count; index++)
            users.Add(infractions[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received a <see cref="InfractionType.Kick" />.
    /// </summary>
    /// <param name="guild">The guild whose kicks to count.</param>
    /// <returns>The total number of users who have received at least one kick in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetDistinctKickedUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(guild, InfractionType.Kick);
        var users = new HashSet<ulong>();

        for (var index = 0; index < infractions.Count; index++)
            users.Add(infractions[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received a <see cref="InfractionType.Mute" /> or a
    ///     <see cref="InfractionType.TemporaryMute" />.
    /// </summary>
    /// <param name="guild">The guild whose mutes to count.</param>
    /// <returns>The total number of users who have received at least one mute in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetDistinctMutedUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> mutes = _infractionService.GetInfractions(guild, InfractionType.Mute);
        IReadOnlyList<Infraction> temporaryMutes = _infractionService.GetInfractions(guild, InfractionType.TemporaryMute);
        var users = new HashSet<ulong>();

        for (var index = 0; index < mutes.Count; index++)
            users.Add(mutes[index].UserId);

        for (var index = 0; index < temporaryMutes.Count; index++)
            users.Add(temporaryMutes[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received a <see cref="InfractionType.Warning" />.
    /// </summary>
    /// <param name="guild">The guild whose warnings to count.</param>
    /// <returns>The total number of users who have received at least one warning in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetDistinctWarnedUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(guild, InfractionType.Warning);
        var users = new HashSet<ulong>();

        for (var index = 0; index < infractions.Count; index++)
            users.Add(infractions[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Gets the remaining total ban duration for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose temporary ban times to sum.</param>
    /// <returns>A <see cref="TimeSpan" /> representing the total remaining time of all temporary bans.</returns>
    public TimeSpan GetRemainingBanTime(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<TemporaryBan> bans = _banService.GetTemporaryBans(guild);
        TimeSpan total = TimeSpan.Zero;

        for (var index = 0; index < bans.Count; index++)
            total += bans[index].ExpiresAt - DateTimeOffset.UtcNow;

        return total;
    }

    /// <summary>
    ///     Gets the remaining total mute duration for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose temporary mute times to sum.</param>
    /// <returns>A <see cref="TimeSpan" /> representing the total remaining time of all temporary mutes.</returns>
    public TimeSpan GetRemainingMuteTime(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Mute> mutes = _muteService.GetTemporaryMutes(guild);
        TimeSpan total = TimeSpan.Zero;

        for (var index = 0; index < mutes.Count; index++)
        {
            if (mutes[index].ExpiresAt is { } expiresAt)
                total += expiresAt - DateTimeOffset.UtcNow;
        }

        return total;
    }

    /// <summary>
    ///     Returns the total number of bans issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose bans to count.</param>
    /// <returns>A tuple containing the total, the temporary, and the permanent, ban count.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public (int Total, int Temporary, int Permanent) GetTotalBanCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        int temporary = _infractionService.GetInfractions(guild, InfractionType.TemporaryBan).Count;
        int permanent = _infractionService.GetInfractions(guild, InfractionType.Ban).Count;

        return (temporary + permanent, temporary, permanent);
    }

    /// <summary>
    ///     Returns the total number of deleted messages in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose deleted messages to count.</param>
    /// <returns>The total number of deleted messages in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async Task<int> GetTotalDeletedMessageCountAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return await _messageDeletionService.CountMessageDeletionsAsync(guild).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the total number of gags issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose gags to count.</param>
    /// <returns>The total number of issued gags in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetTotalGagCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _infractionService.GetInfractions(guild, InfractionType.Gag).Count;
    }

    /// <summary>
    ///     Returns the total number of distinct users who have received an infraction of any kind.
    /// </summary>
    /// <param name="guild">The guild whose infractions to count.</param>
    /// <returns>The total number of users who have received at least one infraction in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetTotalDistinctUsers(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(guild);
        var users = new HashSet<ulong>();

        for (var index = 0; index < infractions.Count; index++)
            users.Add(infractions[index].UserId);

        return users.Count;
    }

    /// <summary>
    ///     Returns the total number of infractions issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose infractions to count.</param>
    /// <returns>The total number of issued infractions in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetTotalInfractionCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _infractionService.GetInfractions(guild).Count;
    }

    /// <summary>
    ///     Returns the total number of gags issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose gags to count.</param>
    /// <returns>The total number of issued gags in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetTotalKickCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _infractionService.GetInfractions(guild, InfractionType.Kick).Count;
    }

    /// <summary>
    ///     Returns the total number of mutes issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose bans to count.</param>
    /// <returns>A tuple containing the total, the temporary, and the permanent, mute count.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public (int Total, int Temporary, int Permanent) GetTotalMuteCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        int temporary = _infractionService.GetInfractions(guild, InfractionType.TemporaryMute).Count;
        int permanent = _infractionService.GetInfractions(guild, InfractionType.Mute).Count;

        return (temporary + permanent, temporary, permanent);
    }

    /// <summary>
    ///     Returns the total number of warnings issued in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose warnings to count.</param>
    /// <returns>The total number of issued warnings in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public int GetTotalWarningCount(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _infractionService.GetInfractions(guild, InfractionType.Warning).Count;
    }

    private static (int A, int B) Ratio(int a, int b)
    {
        int gcd = Gcd(a, b);
        return (a / gcd, b / gcd);
    }

    private static int Gcd(int a, int b)

    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return a == 0 ? b : a;
    }
}
