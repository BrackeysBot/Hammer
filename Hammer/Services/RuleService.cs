using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages guild rules.
/// </summary>
internal sealed class RuleService : BackgroundService
{
    private readonly Dictionary<ulong, List<Rule>> _guildRules = new();
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleService" /> class.
    /// </summary>
    public RuleService(IDbContextFactory<HammerContext> dbContextFactory, ConfigurationService configurationService)
    {
        _dbContextFactory = dbContextFactory;
        _configurationService = configurationService;
    }

    /// <summary>
    ///     Adds a rule to the database.
    /// </summary>
    /// <param name="guild">The guild whose rules to update.</param>
    /// <param name="description">The rule content.</param>
    /// <param name="brief">The rule brief.</param>
    /// <returns>The newly-created rule.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async Task<Rule> AddRuleAsync(DiscordGuild guild, string description, string? brief = null)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));
        if (!_guildRules.TryGetValue(guild.Id, out List<Rule>? rules)) _guildRules.Add(guild.Id, rules = new List<Rule>());

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var rule = new Rule {Id = rules.Count + 1, GuildId = guild.Id, Description = description, Brief = brief};
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
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0xFF0000);
        embed.WithTitle("Rule Not Found");
        embed.WithDescription($"A rule with ID {ruleId} could not be found.");
        return embed;
    }

    /// <summary>
    ///     Creates a "Rule Not Found" embed.
    /// </summary>
    /// <param name="guild">The guild whose branding to display.</param>
    /// <param name="searchQuery">The search query which failed.</param>
    /// <returns>A <see cref="DiscordEmbed" /> stating the rule cannot be found.</returns>
    public DiscordEmbed CreateRuleNotFoundEmbed(DiscordGuild guild, string searchQuery)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0xFF0000);
        embed.WithTitle("Rule Not Found");
        embed.WithDescription($"No rule could be found with the search query `{searchQuery}`.");
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

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
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
    /// <exception cref="RuleNotFoundException">No rule with the specified ID was found.</exception>
    public Rule GetRuleById(ulong guildId, int id)
    {
        if (!GuildHasRule(guildId, id)) throw new RuleNotFoundException(id);

        return _guildRules[guildId].First(r => r.Id == id);
    }

    /// <summary>
    ///     Retrieves a rule.
    /// </summary>
    /// <param name="guild">The guild whose rules to retrieve.</param>
    /// <param name="id">The ID of the rule to retrieve.</param>
    /// <returns>The matching rule, if found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="RuleNotFoundException">No rule with the specified ID was found.</exception>
    public Rule GetRuleById(DiscordGuild guild, int id)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!GuildHasRule(guild, id)) throw new RuleNotFoundException(id);

        return _guildRules[guild.Id].First(r => r.Id == id);
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
    ///     Gets a value indicating whether a rule matches a search query.
    /// </summary>
    /// <param name="rule">The rule to check.</param>
    /// <param name="searchTerms">The search query.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="rule" /> matches <paramref name="searchTerms" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool RuleMatches(Rule rule, IEnumerable<string> searchTerms)
    {
        foreach (string term in searchTerms)
        {
            if (!string.IsNullOrWhiteSpace(rule.Brief))
            {
                foreach (string word in rule.Brief.Split())
                {
                    if (word.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            foreach (string word in rule.Description.Split())
            {
                if (word.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Searches for a rule by a search query.
    /// </summary>
    /// <param name="guild">The guild whose rules to search.</param>
    /// <param name="searchQuery">The query with which to search.</param>
    /// <returns>The first rule that matches the query; or <see langword="null" /> if no rule was found.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="searchQuery" /> is <see langword="null" />.
    /// </exception>
    public Rule? SearchForRule(DiscordGuild guild, string searchQuery)
    {
        if (guild == null) throw new ArgumentNullException(nameof(guild));
        if (searchQuery == null) throw new ArgumentNullException(nameof(searchQuery));
        if (string.IsNullOrWhiteSpace(searchQuery)) return null;

        string[] searchTerms = searchQuery.Split();
        var matches = new List<Rule>();
        IReadOnlyList<Rule> rules = GetGuildRules(guild);

        foreach (Rule item in rules)
        {
            if (RuleMatches(item, searchTerms))
                matches.Add(item);
        }

        return matches.Count > 0 ? matches[0] : null;
    }

    /// <summary>
    ///     Updates a rule's brief.
    /// </summary>
    /// <param name="guild">The guild whose rules to modify.</param>
    /// <param name="ruleId">The ID of the rule to modify.</param>
    /// <param name="brief">The new rule brief.</param>
    public async Task SetRuleBriefAsync(DiscordGuild guild, int ruleId, string? brief)
    {
        if (!GuildHasRule(guild, ruleId)) return;
        Rule rule = GetRuleById(guild, ruleId)!;
        await SetRuleBriefAsync(rule, brief).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates a rule's brief.
    /// </summary>
    /// <param name="rule">The rule to modify.</param>
    /// <param name="brief">The new rule brief.</param>
    public async Task SetRuleBriefAsync(Rule rule, string? brief)
    {
        rule.Brief = brief;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Entry(rule).State = EntityState.Modified;
        context.Update(rule);
        await context.SaveChangesAsync().ConfigureAwait(false);
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
        rule.Description = content;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Entry(rule).State = EntityState.Modified;
        context.Update(rule);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the rules message, if there is any.
    /// </summary>
    public async Task SendRulesMessageAsync(DiscordChannel channel)
    {
        DiscordColor color = DiscordColor.Orange;
        DiscordGuild guild = channel.Guild;
        IReadOnlyList<Rule> rules = GetGuildRules(guild);
        var builder = new DiscordMessageBuilder();
        var index = 0;

        if (_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            color = guildConfiguration.PrimaryColor;

        foreach (Rule[] ruleChunk in rules.Chunk(25)) // embeds cannot have more than 25 fields
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithThumbnail(guild.IconUrl);
            embed.WithAuthor(guild.Name, iconUrl: guild.IconUrl);
            embed.WithTitle("Server Rules");
            embed.WithColor(color);

            if (index == 0)
                embed.WithDescription($"Welcome to {channel.Guild.Name}!\n\n" +
                                      "To ensure that every one of us here are all happy, please take note and follow these " +
                                      "rules:");

            foreach (Rule rule in ruleChunk)
            {
                string name = rule.Brief is { } brief ? $"#{rule.Id} - {brief}" : $"#{rule.Id}";
                embed.AddField(name, rule.Description);
            }

            builder.AddEmbed(embed);

            index++;
        }

        await channel.SendMessageAsync(builder);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return LoadRulesAsync();
    }

    private async Task LoadRulesAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (IGrouping<ulong, Rule> guildRules in context.Rules.AsEnumerable().GroupBy(r => r.GuildId))
        {
            if (!_guildRules.TryGetValue(guildRules.Key, out List<Rule>? rules))
                _guildRules.Add(guildRules.Key, rules = new List<Rule>());

            rules.AddRange(guildRules.OrderBy(r => r.Id));
        }
    }
}
