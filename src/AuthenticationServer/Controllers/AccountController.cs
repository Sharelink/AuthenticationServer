using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using AuthenticationServer.Models;
using BahamutService;
using BahamutService.Model;
using ServerControlService.Service;
using DataLevelDefines;
using System;
using BahamutCommon;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AuthenticationServer.Controllers
{
    public class AccountController : Controller
    {

        [HttpGet]
        public IActionResult Returns(string AccountID,string AccessToken, string APITokenServer,bool NewUser = false)
        {
            return Json(new
            {
                AccountID = AccountID,
                AccessToken = AccessToken,
                APITokenServer = APITokenServer,
                NewUser = NewUser
            });
        }

        [HttpGet]
        public IActionResult RegistReturns(string AccountID, string FinishRegist)
        {
            return PartialView();
        }

        public IActionResult Regist(string appkey)
        {
            return RedirectToAction("RegistReturns", new { AccountID = "147258", FinishRegist = "Yes" });
        }

        //
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string appkey, string accountId = null, string scopes = null, string returnUrl = null)
        {
            ViewData["LoginString"] = accountId;
            ViewData["Appkey"] = appkey;
            ViewData["Scopes"] = scopes;
            ViewData["ReturnUrl"] = returnUrl;
            return PartialView();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model,string appkey, string scopes=null, string returnUrl = null)
        {
            ViewData["LoginString"] = model.LoginString;
            ViewData["Appkey"] = appkey;
            ViewData["Scopes"] = scopes;
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                
                var authService = Startup.ServicesProvider.GetAuthenticationService();
                try
                {
                    var result = authService.LoginValidate(model.LoginString, model.Password);
                    if (result.Succeeded)
                    {
                        var svrCtrlService = Startup.ServicesProvider.GetServerControlManagementService();
                        var mostFreeAppService = svrCtrlService.GetMostFreeAppService(appkey);
                        var mostFreeFileAppService = svrCtrlService.GetMostFreeAppService("b078082bd9d5c63da1b4c5d546a9fa44b6c879b2");
                        dynamic docModel = mostFreeAppService.ServiceDocumentModel;
                        var tokenService = Startup.ServicesProvider.GetTokenService();
                        var newSessionData = new AccountSessionData()
                        {
                            AccountId = result.AccountID,
                            Appkey = appkey,
                            APITokenServer = docModel.APITokenServer
                        };
                        var atokenResult = tokenService.AllocateAccessToken(newSessionData).Result;
                        if (atokenResult == null)
                        {
                            throw new Exception("AllocateAccessToken Failed");
                        }
                        var parameters = new { AccountID = result.AccountID, AccessToken = atokenResult.AccessToken, APITokenServer = atokenResult.APITokenServer};
                        return RedirectToAction("Returns", parameters);
                    }
                }catch (NullReferenceException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView(model);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "Server Error");
                    return PartialView(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return PartialView(model);
        }
    }
}
