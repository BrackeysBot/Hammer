using System;
using BrackeysBot.API.Configuration;
using DisCatSharp.Entities;
using Hammer.Configuration;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages guild configurations.
/// </summary>
internal sealed class ConfigurationService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConfigurationService" /> class.
    /// </summary>
    /// <param name="configuration">The configuration to use.</param>
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    ///     Returns the configuration for a specified guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild whose configuration to retrieve.</param>
    /// <returns>
    ///     A <see cref="GuildConfiguration" /> representing the configuration for the guild. If the guild's configuration does
    ///     not exist, a default configuration is returned.
    /// </returns>
    public GuildConfiguration GetGuildConfiguration(ulong guildId)
    {
        return _configuration.Get<GuildConfiguration>($"guilds.{guildId}") ?? new GuildConfiguration();
    }

    /// <summary>
    ///     Returns the configuration for a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose configuration to retrieve.</param>
    /// <returns>A <see cref="GuildConfiguration" /> representing the configuration for the guild.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public GuildConfiguration GetGuildConfiguration(DiscordGuild guild)
    {
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        return GetGuildConfiguration(guild.Id);
    }
}
