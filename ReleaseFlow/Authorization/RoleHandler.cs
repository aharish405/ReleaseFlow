using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;

namespace ReleaseFlow.Authorization;

public class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;

    public RoleHandler(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        RoleRequirement requirement)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            return;
        }

        var windowsIdentity = user.Identity.Name;
        if (string.IsNullOrEmpty(windowsIdentity))
        {
            return;
        }

        // Get user from database
        var dbUser = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsIdentity && u.IsActive);

        if (dbUser == null)
        {
            return;
        }

        // Check if user's role is in the allowed roles
        if (requirement.AllowedRoles.Contains(dbUser.Role.Name))
        {
            context.Succeed(requirement);
        }
    }
}
