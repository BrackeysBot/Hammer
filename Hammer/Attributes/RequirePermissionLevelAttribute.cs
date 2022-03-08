using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using Hammer.Configuration;
using Hammer.Extensions;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using PermissionLevel = Hammer.Data.PermissionLevel;

namespace Hammer.Attributes;

/// <summary>
///     Defines that usage of this command must require a specified minimum <see cref="PermissionLevel" />.
/// </summary>
internal sealed class RequirePermissionLevelAttribute : CheckBaseAttribute
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly PermissionLevel _permissionLevel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequireContextMenuPermissionLevelAttribute" /> class.
    /// </summary>
    /// <param name="permissionLevel">The minimum permission level.</param>
    public RequirePermissionLevelAttribute(PermissionLevel permissionLevel)
    {
        _permissionLevel = permissionLevel;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        Logger.Info($"{ctx.Member} attempted to run command \"{ctx.Command.Name}\"");
        if (ctx.Guild is null) return Task.FromResult(false);

        var configurationService = ctx.Services.GetRequiredService<ConfigurationService>();
        GuildConfiguration configuration = configurationService.GetGuildConfiguration(ctx.Guild);
        RoleConfiguration roleConfiguration = configuration.RoleConfiguration;

        bool meetsRequirement = ctx.Member.GetPermissionLevel(roleConfiguration) >= _permissionLevel;
        return Task.FromResult(meetsRequirement);
    }
}
