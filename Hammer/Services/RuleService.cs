using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages guild rules.
/// </summary>
internal sealed class RuleService : BackgroundService
{
    private readonly Dictionary<ulong, List<Rule>> _guildRules = new();
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleService" /> class.
    /// </summary>
    public RuleService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    ///     Adds a rule to the database.
    /// </summary>
    /// <param name="guild">The guild whose rules to update.</param>
    /// <param name="ruleContent">The rule content.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<Rule> AddRuleAsync(DiscordGuild guild, string ruleContent)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (string.IsNullOrWhiteSpace(ruleContent)) throw new ArgumentNullException(nameof(ruleContent));
        if (!_guildRules.TryGetValue(guild.Id, out List<Rule>? rules)) _guildRules.Add(guild.Id, rules = new List<Rule>());

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        var rule = new Rule {Id = rules.Count + 1, GuildId = guild.Id, Content = ruleContent};
        EntityEntry<Rule> entry = await context.AddAsync(rule).ConfigureAwait(false);
        rules.Add(rule = entry.Entity);

        await context.SaveChangesAsync().ConfigureAwait(false);
        return rule;
    }

    /// <summary>
    ///     Creates a "Rule Not Found" embed.
    /// </summary>
    /// <param name="guild">The guild whose branding to display.</param>
    /// <param name="ruleId">The ID of the rule which wasn't found.</param>
    /// <returns>A <see cref="DiscordEmbed" /> stating the rule cannot be found.</returns>
    public DiscordEmbed CreateRuleNotFoundEmbed(DiscordGuild guild, int ruleId)
    {
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithColor(0xFF0000);
        embed.WithTitle("Rule Not Found");
        embed.WithDescription($"A rule with ID {ruleId} could not be found.");
        return embed;
    }

    /// <summary>
    ///     Deletes a rule from the database.
    /// </summary>
    /// <param name="guild">The guild whose rules to update.</param>
    /// <param name="id">The ID of the rule to delete.</param>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="id" /> is less than 1, or greater than the count of the guild rules.
    /// </exception>
    public async Task DeleteRuleAsync(DiscordGuild guild, int id)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!GuildHasRule(guild, id)) return;

        Rule ruleToDelete = GetRuleById(guild, id)!;
        _guildRules[guild.Id].Remove(ruleToDelete);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        context.RemoveRange(context.Rules.Where(r => r.GuildId == guild.Id));

        // propagate IDs downwards
        IReadOnlyList<Rule> remainder = GetGuildRules(guild);
        for (var index = 0; index < remainder.Count; index++)
        {
            Rule rule = remainder[index];
            rule.Id = index + 1;
            await context.AddAsync(rule).ConfigureAwait(false);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Retrieves a rule.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose rules to retrieve.</param>
    /// <param name="id">The ID of the rule to retrieve.</param>
    /// <returns>The matching rule, if found.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="id" /> is less than 1, or greater than the count of the guild rules.
    /// </exception>
    public Rule? GetRuleById(ulong guildId, int id)
    {
        if (!GuildHasRule(guildId, id)) return null;

        return _guildRules[guildId].FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    ///     Retrieves a rule.
    /// </summary>
    /// <param name="guild">The guild whose rules to retrieve.</param>
    /// <param name="id">The ID of the rule to retrieve.</param>
    /// <returns>The matching rule, if found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="id" /> is less than 1, or greater than the count of the guild rules.
    /// </exception>
    public Rule? GetRuleById(DiscordGuild guild, int id)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!GuildHasRule(guild, id)) return null;

        return _guildRules[guild.Id].FirstOrDefault(r => r.Id == id);
    }

    /// <summary>
    ///     Gets all the rules associated with a specific guild.
    /// </summary>
    /// <param name="guild">The guild whose rules to search.</param>
    /// <returns>A read-only view of the rules associated with <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Rule> GetGuildRules(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!_guildRules.TryGetValue(guild.Id, out List<Rule>? rules))
            return ArraySegment<Rule>.Empty;

        return rules.OrderBy(r => r.Id).ToArray();
    }

    /// <summary>
    ///     Determines if the specified guild has any rules defined.
    /// </summary>
    /// <param name="guildId">The ID of the guild to check.</param>
    /// <param name="id">The ID of the rule to search for.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="guildId" /> has a specified rule defined; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool GuildHasRule(ulong guildId, int id)
    {
        if (id < 1) return false;

        if (!_guildRules.TryGetValue(guildId, out List<Rule>? rules))
            return false;

        return id <= rules.Count && rules.Exists(r => r.Id == id);
    }

    /// <summary>
    ///     Determines if the specified guild has any rules defined.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <param name="id">The ID of the rule to search for.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="guild" /> has a specified rule defined; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public bool GuildHasRule(DiscordGuild guild, int id)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (id < 1) return false;

        if (!_guildRules.TryGetValue(guild.Id, out List<Rule>? rules))
            return false;

        return id <= rules.Count && rules.Exists(r => r.Id == id);
    }

    /// <summary>
    ///     Updates a rule's content.
    /// </summary>
    /// <param name="guild">The guild whose rules to modify.</param>
    /// <param name="ruleId">The ID of the rule to modify.</param>
    /// <param name="content">The new rule content.</param>
    public async Task SetRuleContentAsync(DiscordGuild guild, int ruleId, string content)
    {
        if (!GuildHasRule(guild, ruleId)) return;
        Rule rule = GetRuleById(guild, ruleId)!;
        await SetRuleContentAsync(rule, content).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates a rule's content.
    /// </summary>
    /// <param name="rule">The rule to modify.</param>
    /// <param name="content">The new rule content.</param>
    public async Task SetRuleContentAsync(Rule rule, string content)
    {
        rule.Content = content;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        context.Entry(rule).State = EntityState.Modified;
        context.Update(rule);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        foreach (IGrouping<ulong, Rule> guildRules in context.Rules.AsEnumerable().GroupBy(r => r.GuildId))
        {
            if (!_guildRules.TryGetValue(guildRules.Key, out List<Rule>? rules))
                _guildRules.Add(guildRules.Key, rules = new List<Rule>());

            rules.AddRange(guildRules.OrderBy(r => r.Id));
        }
    }
}
