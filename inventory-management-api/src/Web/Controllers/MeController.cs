using Inventory_Management.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Microsoft.Extensions.DependencyInjection.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MeController : ControllerBase
{
    private readonly IUser _user;

    public MeController(IUser user)
    {
        _user = user;
    }

    [HttpGet]
    [Authorize(Policy = Auth0Defaults.UserPolicy)]
    [SwaggerOperation(Summary = "Get current user", Description = "Return the user id and claims from the Auth0 access token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        return Ok(new
        {
            UserId = _user.Id,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
