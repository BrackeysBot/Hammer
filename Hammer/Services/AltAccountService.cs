using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages alt accounts.
/// </summary>
internal sealed class AltAccountService : BackgroundService
{
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly DiscordLogService _discordLogService;
    private readonly ConcurrentDictionary<ulong, HashSet<ulong>> _altAccountCache = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="AltAccountService" /> class.
    /// </summary>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="discordLogService">The Discord log service.</param>
    public AltAccountService(IDbContextFactory<HammerContext> dbContextFactory, DiscordLogService discordLogService)
    {
        _dbContextFactory = dbContextFactory;
        _discordLogService = discordLogService;
    }

    /// <summary>
    ///     Gets the alt accounts for the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="alt">The alt.</param>
    /// <param name="staffMember">The staff member.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="user" /> or <paramref name="alt" /> is <see langword="null" />.
    /// </exception>
    public void AddAlt(DiscordUser user, DiscordUser alt, DiscordMember staffMember)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (alt is null) throw new ArgumentNullException(nameof(alt));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        using HammerContext context = _dbContextFactory.CreateDbContext();
        var record = new AltAccount {StaffMemberId = staffMember.Id, RegisteredAt = DateTimeOffset.UtcNow};
        context.AltAccounts.Add(record with {UserId = user.Id, AltId = alt.Id});
        context.AltAccounts.Add(record with {UserId = alt.Id, AltId = user.Id});
        context.SaveChanges();

        HashSet<ulong> cache = _altAccountCache.GetOrAdd(user.Id, new HashSet<ulong>());
        cache.Add(alt.Id);

        foreach (ulong altId in cache)
        {
            HashSet<ulong> altCache = _altAccountCache.GetOrAdd(altId, new HashSet<ulong>());
            altCache.Add(user.Id);
        }

        AltAccount[] altAccounts = context.AltAccounts.Where(a => a.AltId == user.Id).ToArray();

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user.GetUsernameWithDiscriminator(), iconUrl: user.GetAvatarUrl(ImageFormat.Png));
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Alt account registered");
        embed.WithDescription("The following users have been registered as alts of each other.");
        embed.AddField("Main Account", user.Mention, true);
        embed.AddField($"Alt {"Account".ToQuantity(altAccounts.Length, ShowQuantityAs.None)}",
            string.Join("\n", altAccounts.Select(a => MentionUtility.MentionUser(a.UserId))), true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        _ = _discordLogService.LogAsync(staffMember.Guild, embed);
    }

    /// <summary>
    ///     Gets the alt accounts for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The alt accounts.</returns>
    public IReadOnlyCollection<ulong> GetAltsFor(ulong userId)
    {
        if (_altAccountCache.TryGetValue(userId, out HashSet<ulong>? alts))
        {
            // get alts of alts without this userId
            return alts.Concat(alts.SelectMany(a => _altAccountCache[a]).Where(a => a != userId)).ToArray();
        }

        return ArraySegment<ulong>.Empty;
    }

    /// <summary>
    ///     Gets the alt accounts for the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="alt">The alt.</param>
    /// <param name="staffMember">The staff member.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="user" /> or <paramref name="alt" /> is <see langword="null" />.
    /// </exception>
    public void RemoveAlt(DiscordUser user, DiscordUser alt, DiscordMember staffMember)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (alt is null) throw new ArgumentNullException(nameof(alt));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        using HammerContext context = _dbContextFactory.CreateDbContext();

        AltAccount? altAccount = context.AltAccounts.FirstOrDefault(a => a.UserId == user.Id && a.AltId == alt.Id);
        if (altAccount is not null) context.AltAccounts.Remove(altAccount);

        AltAccount[] altAccounts = context.AltAccounts.Where(a => a.AltId == user.Id).ToArray();
        if (altAccounts.Length > 0) context.AltAccounts.RemoveRange(altAccounts);

        HashSet<ulong> cache = _altAccountCache.GetOrAdd(user.Id, new HashSet<ulong>());
        HashSet<ulong>? altCache = _altAccountCache.GetOrAdd(alt.Id, new HashSet<ulong>());
        cache.Remove(alt.Id);
        altCache.Remove(user.Id);

        foreach (ulong altId in GetAltsFor(alt.Id))
        {
            altAccounts = context.AltAccounts.Where(a => a.UserId == user.Id && a.AltId == altId).ToArray();
            if (altAccounts.Length > 0) context.AltAccounts.RemoveRange(altAccounts);
            cache.Remove(altId);
            if (_altAccountCache.TryGetValue(altId, out altCache))
            {
                altCache.Remove(user.Id);
            }
        }

        context.SaveChanges();

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user.GetUsernameWithDiscriminator(), iconUrl: user.GetAvatarUrl(ImageFormat.Png));
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Alt account unregistered");
        embed.WithDescription("The following users have been unregistered as alts of each other.");
        embed.AddField("Main Account", user.Mention, true);
        embed.AddField("Alt Account", alt.Mention, true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        _ = _discordLogService.LogAsync(staffMember.Guild, embed);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UpdateFromDatabase();
        return Task.CompletedTask;
    }

    private void UpdateFromDatabase()
    {
        using HammerContext context = _dbContextFactory.CreateDbContext();
        foreach (IGrouping<ulong, AltAccount> group in context.AltAccounts.GroupBy(u => u.UserId))
        {
            HashSet<ulong> cache = _altAccountCache.GetOrAdd(group.Key, new HashSet<ulong>());
            foreach (AltAccount altAccount in group)
            {
                HashSet<ulong> altCache = _altAccountCache.GetOrAdd(altAccount.AltId, new HashSet<ulong>());
                cache.Add(altAccount.AltId);
                altCache.Add(group.Key);
            }
        }
    }
}
