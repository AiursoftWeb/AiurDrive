using System.Diagnostics;
using Aiursoft.AiurDrive.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.AiurDrive.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
        }
    }
}