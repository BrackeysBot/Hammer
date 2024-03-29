﻿using DSharpPlus.Entities;
using Hammer.Services;

namespace Hammer.Data;

/// <summary>
///     Represents a response to the <c>View Infraction History</c> command.
/// </summary>
internal sealed class InfractionHistoryResponse
{
    private readonly InfractionService _infractionService;
    private readonly InfractionSearchOptions _searchOptions;

    private int _page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionHistoryResponse" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="targetUser">The user whose infractions are being displayed.</param>
    /// <param name="user">The user who triggered this response.</param>
    /// <param name="guild">The guild in which the history was requested.</param>
    /// <param name="staffRequested">
    ///     <see langword="true" /> if a staff member requested the history; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="searchOptions">A structure containing options to filter the search results.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="infractionService" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="targetUser" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    /// </exception>
    public InfractionHistoryResponse(
        InfractionService infractionService,
        DiscordUser targetUser,
        DiscordUser user,
        DiscordGuild guild,
        bool staffRequested,
        InfractionSearchOptions searchOptions = default
    )
    {
        ArgumentNullException.ThrowIfNull(infractionService);
        ArgumentNullException.ThrowIfNull(targetUser);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        _infractionService = infractionService;
        _searchOptions = searchOptions;
        TargetUser = targetUser;
        User = user;
        Guild = guild;
        StaffRequested = staffRequested;
    }

    /// <summary>
    ///     Gets the guild to which this infraction applies.
    /// </summary>
    /// <value>The guild.</value>
    public DiscordGuild Guild { get; }

    /// <summary>
    ///     Gets the ID of this response.
    /// </summary>
    /// <value>The response ID.</value>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets the count of infractions for <see cref="TargetUser" />.
    /// </summary>
    /// <value>The infraction count.</value>
    public int InfractionCount => _infractionService.GetInfractionCount(TargetUser, Guild, _searchOptions);

    /// <summary>
    ///     Gets or sets the zero-based page index of infractions to display.
    /// </summary>
    /// <value>The page index.</value>
    public int Page
    {
        get => _page;
        set => _page = Math.Clamp(value, 0, Pages);
    }

    /// <summary>
    ///     Gets the total number of pages.
    /// </summary>
    /// <value>The page count.</value>
    public int Pages
    {
        get
        {
            var pages = (int) Math.Ceiling(InfractionCount / 10.0);
            return pages < 1 ? 1 : pages;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether a staff member requested this history.
    /// </summary>
    /// <value><see langword="true" /> if a staff member requested this history; otherwise, <see langword="false" />.</value>
    public bool StaffRequested { get; }

    /// <summary>
    ///     Gets the user whose infractions are being displayed.
    /// </summary>
    /// <value>The target user.</value>
    public DiscordUser TargetUser { get; }

    /// <summary>
    ///     Gets the user who triggered this response.
    /// </summary>
    /// <value>The user.</value>
    public DiscordUser User { get; }
}
