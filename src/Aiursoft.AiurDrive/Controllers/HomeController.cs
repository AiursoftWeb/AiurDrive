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
        return this.SimpleView(new IndexViewModel());
    }

    public IActionResult SelfHost()
    {
        return this.SimpleView(new IndexViewModel());
    }
}
