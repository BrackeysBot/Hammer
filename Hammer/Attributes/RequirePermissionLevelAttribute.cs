using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using Hammer.Configuration;
using Hammer.Extensions;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using PermissionLevel = Hammer.Data.PermissionLevel;

namespace Hammer.Attributes;

/// <summary>
///     Defines that usage of this command must require a specified minimum <see cref="PermissionLevel" />.
/// </summary>
internal sealed class RequirePermissionLevelAttribute : CheckBaseAttribute
{
    private readonly PermissionLevel _permissionLevel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequirePermissionLevelAttribute" /> class.
    /// </summary>
    /// <param name="permissionLevel">The minimum permission level.</param>
    public RequirePermissionLevelAttribute(PermissionLevel permissionLevel)
    {
        _permissionLevel = permissionLevel;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (ctx.Guild is null) return Task.FromResult(false);

        var configurationService = ctx.Services.GetRequiredService<ConfigurationService>();
        GuildConfiguration configuration = configurationService.GetGuildConfiguration(ctx.Guild);
        RoleConfiguration roleConfiguration = configuration.RoleConfiguration;

        bool meetsRequirement = ctx.Member.GetPermissionLevel(roleConfiguration) >= _permissionLevel;
        return Task.FromResult(meetsRequirement);
    }
}
