using Hammer.Data;
using Hammer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.Controllers;

[ApiController]
[Route("[controller]")]
internal sealed class UserController : ControllerBase
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserController" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public UserController(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [HttpGet(Name = "infractions")]
    public IEnumerable<Infraction> GetInfractions(ulong userId, ulong guildId)
    {
        return _infractionService.EnumerateInfractions(userId, guildId);
    }
}
