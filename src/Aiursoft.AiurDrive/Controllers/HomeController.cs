using Aiursoft.AiurDrive.Models.HomeViewModels;
using Aiursoft.AiurDrive.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers;

[LimitPerMin]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Drive");
        }
        
        return this.SimpleView(new IndexViewModel());
    }
}
