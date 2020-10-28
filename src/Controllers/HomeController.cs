﻿using Aiursoft.Colossus.Models;
using Aiursoft.Colossus.Models.HomeViewModels;
using Aiursoft.Gateway.SDK.Services;
using Aiursoft.Handler.Attributes;
using Aiursoft.Identity;
using Aiursoft.Identity.Attributes;
using Aiursoft.XelNaga.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Aiursoft.Colossus.Controllers
{
    [LimitPerMin]
    public class HomeController : Controller
    {
        private readonly SignInManager<ColossusUser> _signInManager;
        private readonly GatewayLocator _gatewayLocator;
        private const int DefaultSize = 30 * 1024 * 1024;

        public HomeController(
            SignInManager<ColossusUser> signInManager,
            GatewayLocator gatewayLocator)
        {
            _signInManager = signInManager;
            _gatewayLocator = gatewayLocator;
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
            return this.SignOutRootServer(_gatewayLocator.Endpoint, new AiurUrl(string.Empty, "Home", nameof(Index), new { }));
        }
    }
}
