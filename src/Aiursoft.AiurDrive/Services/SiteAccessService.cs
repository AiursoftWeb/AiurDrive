using System.Security.Claims;
using Aiursoft.AiurDrive.Authorization;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.Scanner.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Services;

public class SiteAccessService(
    AiurDriveDbContext context,
    UserManager<User> users,
    RoleManager<IdentityRole> roles,
    IAuthorizationService authorization) : IScopedDependency
{
    public const string AdministrationPolicy = "SiteAdministration";

    public async Task<bool> CanManageAsync(Site site, User user, ClaimsPrincipal principal) =>
        site.AppUserId == user.Id ||
        (await authorization.AuthorizeAsync(principal, AppPermissionNames.CanManageAnySite)).Succeeded;

    public async Task<bool> CanAccessAsync(Site site, User user, ClaimsPrincipal principal,
        SharePermission requiredPermission)
    {
        if (await CanManageAsync(site, user, principal)) return true;

        var userRoles = await users.GetRolesAsync(user);
        var roleIds = await roles.Roles.Where(r => userRoles.Contains(r.Name!))
            .Select(r => r.Id).ToListAsync();
        var share = await context.SiteShares.Where(s => s.SiteId == site.Id)
            .Where(s => s.SharedWithUserId == user.Id ||
                        (s.SharedWithRoleId != null && roleIds.Contains(s.SharedWithRoleId)))
            .OrderByDescending(s => s.Permission).FirstOrDefaultAsync();

        return share != null && (requiredPermission == SharePermission.ReadOnly ||
            requiredPermission == SharePermission.Editable && share.Permission == SharePermission.Editable);
    }
}
