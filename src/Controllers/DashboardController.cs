﻿using Aiursoft.Archon.SDK.Services;
using Aiursoft.Colossus.Models;
using Aiursoft.Colossus.Models.DashboardViewModels;
using Aiursoft.Handler.Attributes;
using Aiursoft.Handler.Exceptions;
using Aiursoft.Handler.Models;
using Aiursoft.Identity.Attributes;
using Aiursoft.Probe.SDK.Services.ToProbeServer;
using Aiursoft.XelNaga.Services;
using Aiursoft.XelNaga.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.Colossus.Controllers
{
    [LimitPerMin]
    [AiurForceAuth]
    [Route("Dashboard")]
    public class DashboardController : Controller
    {
        private readonly SitesService _sitesService;
        private readonly AppsContainer _appsContainer;
        private readonly UserManager<ColossusUser> _userManager;
        private readonly FoldersService _foldersService;
        private readonly FilesService _filesService;
        private readonly AiurCache _cache;

        private Task<string> AccessToken => _appsContainer.AccessToken();

        public DashboardController(
            SitesService sitesService,
            AppsContainer appsContainer,
            UserManager<ColossusUser> userManager,
            FoldersService foldersService,
            FilesService filesService,
            AiurCache cache)
        {
            _sitesService = sitesService;
            _appsContainer = appsContainer;
            _userManager = userManager;
            _foldersService = foldersService;
            _filesService = filesService;
            _cache = cache;
        }

        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            var sites = await _sitesService.ViewMySitesAsync(await AccessToken);
            if (string.IsNullOrEmpty(user.SiteName) || !sites.Sites.Any(t => t.SiteName == user.SiteName))
            {
                return RedirectToAction(nameof(CreateSite));
            }
            var model = new IndexViewModel(user)
            {
                SiteName = user.SiteName
            };
            return View(model);
        }

        [Route("CreateSite")]
        public async Task<IActionResult> CreateSite()
        {
            var user = await GetCurrentUserAsync();
            var sites = await _sitesService.ViewMySitesAsync(await AccessToken);
            if (!string.IsNullOrEmpty(user.SiteName) && sites.Sites.Any(t => t.SiteName == user.SiteName))
            {
                return RedirectToAction(nameof(Index));
            }
            var model = new CreateSiteViewModel(user)
            {
                SiteName = user.NickName.Replace(" ", "").ToLower() + "s-site"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CreateSite")]
        public async Task<IActionResult> CreateSite(CreateSiteViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                var existing = await _sitesService.ViewMySitesAsync(await AccessToken);
                if (existing.Sites.Any(t => t.SiteName.ToLower().Trim() == model.SiteName.ToLower().Trim()))
                {
                    ModelState.AddModelError(string.Empty, $"The site with name: {model.SiteName} already exists. Please try another name.");
                    model.Recover(user);
                    return View(model);
                }
                await _sitesService.CreateNewSiteAsync(await AccessToken, model.SiteName, model.OpenToUpload, model.OpenToDownload);
                user.SiteName = model.SiteName;
                await _userManager.UpdateAsync(user);
                return RedirectToAction(nameof(Index));
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                return View(model);
            }
        }

        [Route("ViewFiles/{**path}")]
        public async Task<IActionResult> ViewFiles(string path, bool justHaveUpdated)
        {
            var user = await GetCurrentUserAsync();
            if (string.IsNullOrWhiteSpace(user.SiteName))
            {
                return RedirectToAction(nameof(CreateSite));
            }
            try
            {
                var data = await _foldersService.ViewContentAsync(await AccessToken, user.SiteName, path);
                var model = new ViewFilesViewModel(user)
                {
                    Folder = data.Value,
                    Path = path,
                    SiteName = user.SiteName,
                    JustHaveUpdated = justHaveUpdated
                };
                return View(model);
            }
            catch (AiurUnexpectedResponse e) when (e.Code == ErrorType.NotFound)
            {
                return NotFound();
            }
        }

        [Route("NewFolder/{**path}")]
        public async Task<IActionResult> NewFolder(string path)
        {
            var user = await GetCurrentUserAsync();
            var model = new NewFolderViewModel(user)
            {
                Path = path
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("NewFolder/{**path}")]
        public async Task<IActionResult> NewFolder(NewFolderViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                await _foldersService.CreateNewFolderAsync(await AccessToken, user.SiteName, model.Path, model.NewFolderName, false);
                return RedirectToAction(nameof(ViewFiles), new { path = model.Path });
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                return View(model);
            }
        }

        [Route("DeleteFolder/{**path}")]
        public async Task<IActionResult> DeleteFolder([FromRoute] string path)
        {
            var user = await GetCurrentUserAsync();
            var model = new DeleteFolderViewModel(user)
            {
                Path = path
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("DeleteFolder/{**path}")]
        public async Task<IActionResult> DeleteFolder(DeleteFolderViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                await _foldersService.DeleteFolderAsync(await AccessToken, user.SiteName, model.Path);
                return RedirectToAction(nameof(ViewFiles), new { path = model.Path.DetachPath() });
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                return View(model);
            }
        }

        [Route("DeleteFile/{**path}")]
        public async Task<IActionResult> DeleteFile([FromRoute] string path)
        {
            var user = await GetCurrentUserAsync();
            var model = new DeleteFileViewModel(user)
            {
                Path = path
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("DeleteFile/{**path}")]
        public async Task<IActionResult> DeleteFile(DeleteFileViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                await _filesService.DeleteFileAsync(await AccessToken, user.SiteName, model.Path);
                return RedirectToAction(nameof(ViewFiles), new { path = model.Path.DetachPath() });
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                return View(model);
            }
        }

        [Route("CloneFile/{**path}")]
        public async Task<IActionResult> CloneFile([FromRoute] string path)
        {
            var user = await GetCurrentUserAsync();
            await _filesService.CopyFileAsync(await AccessToken, user.SiteName, path, user.SiteName, path.DetachPath());
            return RedirectToAction(nameof(ViewFiles), new { path = path.DetachPath() });
        }

        [Route("Delete")]
        public async Task<IActionResult> Delete()
        {
            var user = await GetCurrentUserAsync();
            var model = new DeleteViewModel(user)
            {
                SiteName = user.SiteName
            };
            return View(model);
        }

        [Route("Delete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeleteViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                await _sitesService.DeleteSiteAsync(await AccessToken, user.SiteName);
                user.SiteName = string.Empty;
                await _userManager.UpdateAsync(user);
                return RedirectToAction(nameof(CreateSite));
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                return View(model);
            }
        }

        [Route("Settings")]
        public async Task<IActionResult> Settings(bool justHaveUpdated)
        {
            var user = await GetCurrentUserAsync();
            var sites = await _sitesService.ViewMySitesAsync(await AccessToken);
            var hasASite = !string.IsNullOrEmpty(user.SiteName) && sites.Sites.Any(t => t.SiteName == user.SiteName);
            if (hasASite)
            {
                var siteDetail = await _sitesService.ViewSiteDetailAsync(await AccessToken, user.SiteName);
                var model = new SettingsViewModel(user)
                {
                    SiteSize = siteDetail.Size,
                    HasASite = true,
                    JustHaveUpdated = justHaveUpdated,
                    NewSiteName = siteDetail.Site.SiteName,
                    OldSiteName = siteDetail.Site.SiteName,
                    OpenToDownload = siteDetail.Site.OpenToDownload,
                    OpenToUpload = siteDetail.Site.OpenToUpload
                };
                return View(model);
            }
            else
            {
                var model = new SettingsViewModel(user)
                {
                    HasASite = false,
                    JustHaveUpdated = justHaveUpdated
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Settings")]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                model.Recover(user);
                return View(model);
            }
            try
            {
                var token = await _appsContainer.AccessToken();
                await _sitesService.UpdateSiteInfoAsync(token, model.OldSiteName, model.NewSiteName, model.OpenToUpload, model.OpenToDownload);
                user.SiteName = model.NewSiteName;
                await _userManager.UpdateAsync(user);
                _cache.Clear($"site-public-status-{model.OldSiteName}");
                _cache.Clear($"site-public-status-{model.NewSiteName}");
                return RedirectToAction(nameof(Settings), "Dashboard", new { JustHaveUpdated = true });
            }
            catch (AiurUnexpectedResponse e)
            {
                ModelState.AddModelError(string.Empty, e.Response.Message);
                model.Recover(user);
                model.NewSiteName = model.OldSiteName;
                return View(model);
            }
        }

        private async Task<ColossusUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }
    }
}
