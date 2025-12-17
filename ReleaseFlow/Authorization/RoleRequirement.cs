using Microsoft.AspNetCore.Authorization;

namespace ReleaseFlow.Authorization;

public class RoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}
