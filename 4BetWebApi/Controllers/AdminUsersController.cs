using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly FourBetDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public AdminUsersController(FourBetDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                Role = u.Role.ToString(),
                u.IsEmailVerified,
                u.IsBdVerified
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new { message = "Role is required." });
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var targetRole))
        {
            return BadRequest(new { message = "Role must be User, Moderator, or Admin." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.Role = targetRole;
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(
            action: "UserRoleChanged",
            entityType: "User",
            entityId: user.Id,
            userId: GetActorUserId(),
            summary: $"User role updated to {targetRole}.",
            payload: new { TargetUser = user.Email, targetRole });

        return Ok(new { message = $"Role updated to {targetRole}." });
    }

    private Guid? GetActorUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId)
                    ?? User.FindFirstValue("nameid");
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
