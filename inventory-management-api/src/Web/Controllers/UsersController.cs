using Inventory_Management.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Users.Commands.UpdateAccess;
using Microsoft.Extensions.DependencyInjection.Users.Queries.GetCollection;
using Microsoft.Extensions.DependencyInjection.Users.Queries.GetSingle;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory_Management.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.Manager}")]
    [SwaggerOperation(Summary = "List users", Description = "Everyone who has logged in, newest first. NewOnly lists users still waiting for a role.")]
    [ProducesResponseType(typeof(GetUsersResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        return Ok(await _mediator.Send(query));
    }

    // No role needed: a new user calls this to find out they're waiting for access.
    [HttpGet("me")]
    [Authorize(Policy = Auth0Defaults.UserPolicy)]
    [SwaggerOperation(Summary = "Get the signed-in user", Description = "Role (null while waiting for access) and warehouse permissions.")]
    [ProducesResponseType(typeof(MeDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        return Ok(await _mediator.Send(new GetMeQuery()));
    }

    [HttpPut("{uid}/access")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.Manager}")]
    [SwaggerOperation(Summary = "Set a user's role and warehouses", Description = "Saves, then emails the user their role, duties and warehouses.")]
    [ProducesResponseType(typeof(UpdateUserAccessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccess(Guid uid, [FromBody] UpdateUserAccessCommand command)
    {
        command.UserUid = uid;
        return Ok(await _mediator.Send(command));
    }
}
