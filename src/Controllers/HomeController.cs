using Aiursoft.AiurDrive.Models;
using Aiursoft.AiurDrive.Models.HomeViewModels;
using Aiursoft.Identity;
using Aiursoft.Identity.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.Directory.SDK.Configuration;
using Microsoft.Extensions.Options;

namespace Aiursoft.AiurDrive.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<AiurDriveUser> _signInManager;
        private readonly DirectoryConfiguration _directoryLocator;
        private const int DefaultSize = 30 * 1024 * 1024;

        public HomeController(
            SignInManager<AiurDriveUser> signInManager,
            IOptions<DirectoryConfiguration> DirectoryLocator)
        {
            _signInManager = signInManager;
            _directoryLocator = DirectoryLocator.Value;
        }

        [AiurForceAuth(preferController: "Dashboard", preferAction: "Index", justTry: true)]
        public IActionResult Index()
        {
            var model = new IndexViewModel
            {
                MaxSize = DefaultSize
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return this.SignOutRootServer(_directoryLocator.Instance, $"Home/{nameof(Index)}");
        }
    }
}
