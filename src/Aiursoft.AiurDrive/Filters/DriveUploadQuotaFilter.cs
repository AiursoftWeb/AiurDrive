using System.Security.Claims;
using Aiursoft.AiurDrive.Configuration;
using Aiursoft.AiurDrive.Controllers;
using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Services.FileStorage;
using Aiursoft.Scanner.Abstractions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Aiursoft.AiurDrive.Filters;

// Site quotas belong to AiurDrive; the shared file controller remains business-independent.
public class DriveUploadQuotaFilter(
    StorageService storage,
    DriveStorageQuota quota,
    AiurDriveDbContext database,
    GlobalSettingsService settings,
    IStringLocalizer<DriveUploadQuotaFilter> localizer) : IAsyncActionFilter, IAsyncResourceFilter, IScopedDependency
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor action &&
            action.ControllerTypeInfo.AsType() == typeof(VaultApiController) &&
            action.ActionName == nameof(VaultApiController.Upload) &&
            context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId)
        {
            // Serialize before model binding buffers IFormFile bodies, not just before disk writes.
            await using var lease = await quota.LockAsync("vault-upload:" + userId, context.HttpContext.RequestAborted);
            await next();
            return;
        }
        await next();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() != typeof(FilesController) ||
            action.ActionName is not (nameof(FilesController.Upload) or nameof(FilesController.UploadPrivate)))
        {
            await next();
            return;
        }

        var isPrivate = action.ActionName == nameof(FilesController.UploadPrivate);
        context.ActionArguments.TryGetValue("subfolder", out var pathArgument);
        context.ActionArguments.TryGetValue("token", out var tokenArgument);
        var path = pathArgument as string;
        var token = tokenArgument as string;
        if (path == null || token == null ||
            !storage.TryValidateToken(path, token, FilePermission.Upload, isPrivate, out var grant) ||
            (grant.UploadPolicy!.RequireAuthenticatedUser && context.HttpContext.User.Identity?.IsAuthenticated != true))
        {
            // The standard controller reports invalid or expired upload grants.
            await next();
            return;
        }

        var siteName = path.Replace('\\', '/').Split('/')[0];
        var site = await database.Sites.AsNoTracking()
            .SingleOrDefaultAsync(s => s.SiteName == siteName, context.HttpContext.RequestAborted);
        if (site == null)
        {
            // AiurDrive private uploads belong to an existing site. A stale grant must not
            // recreate an unmetered folder after its site has been deleted or renamed.
            if (isPrivate) context.Result = new NotFoundResult();
            else await next();
            return;
        }

        await using var lease = await quota.LockAsync("site:" + site.Id, context.HttpContext.RequestAborted);
        if (!await database.Sites.AsNoTracking().AnyAsync(s => s.Id == site.Id && s.SiteName == siteName,
                context.HttpContext.RequestAborted))
        {
            context.Result = new NotFoundResult();
            return;
        }
        var request = context.HttpContext.Request;
        var maxRequest = checked(grant.UploadPolicy.MaxBytes + 64 * 1024);
        if (request.ContentLength > maxRequest)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }
        var bodyLimit = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = maxRequest;

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(new FormOptions
            {
                MultipartBodyLengthLimit = maxRequest,
                MultipartHeadersLengthLimit = 16 * 1024,
                ValueLengthLimit = 16 * 1024
            }, context.HttpContext.RequestAborted);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
            return;
        }

        var limit = site.StorageSizeLimit ?? await settings.GetIntSettingAsync(SettingsMap.MaxSiteStorageInGB);
        try
        {
            // Standard uploads create a new name on collision, so charge the entire incoming file.
            quota.Check(checked(limit * DriveStorageQuota.GiB), form.Files.Sum(file => file.Length),
                storage.GetFilePhysicalPath(site.SiteName),
                storage.GetFilePhysicalPath(site.SiteName, isVault: true));
        }
        catch (StorageQuotaExceededException)
        {
            context.Result = new ObjectResult(localizer["Storage quota exceeded."].Value)
                { StatusCode = StatusCodes.Status413PayloadTooLarge };
            return;
        }
        // Hold the lease through the actual write; checking only before it races other uploads.
        await next();
    }
}
