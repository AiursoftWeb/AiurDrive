using System.Diagnostics;
using Aiursoft.AiurDrive.Models.ErrorViewModels;
using Aiursoft.AiurDrive.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

/// <summary>
/// This controller is used to show error pages.
/// </summary>
public class ErrorController : Controller
{
    [Route("Error/Code{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Code(int code, [FromQuery] string? returnUrl = null)
    {
        var pageTitle = code switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Access Denied",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => $"Error {code}"
        };

        var model = new ErrorViewModel(pageTitle)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorCode = code,
            ReturnUrl = returnUrl
        };

        return this.SimpleView(model, "Error");
    }
}
