using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Hammer.API;
using Hammer.Data;
using Hammer.Data.Infractions;
using Hammer.Extensions;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages member warnings.
/// </summary>
internal sealed class WarningService
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarningService" /> class.
    /// </summary>
    public WarningService(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    /// <summary>
    ///     Warns a user with the specified reason.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="staffMember">The staff member who issued the warning.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.
    /// </exception>
    public Task<Infraction> WarnAsync(DiscordUser user, DiscordMember staffMember, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason));

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace()
        };

        return _infractionService.CreateInfractionAsync(InfractionType.Warning, user, staffMember, options);
    }
}
