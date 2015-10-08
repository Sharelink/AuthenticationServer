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
        public IActionResult Returns(string AccountID,string AccessToken, string AppServerIP,int AppServerPort, string AppServiceUrl)
        {
            return Json(new
            {
                AccountID = AccountID,
                AccessToken = AccessToken,
                AppServerIP = AppServerIP,
                AppServerPort = AppServerPort,
                AppServiceUrl = AppServiceUrl
            });
        }

        [HttpGet]
        public IActionResult RegistReturns(string AccountID, string FinishRegist)
        {
            return PartialView();
        }

        [HttpGet]
        public IActionResult Regist(string appkey)
        {
            return RedirectToAction("RegistReturns", new { AccountID = "147258", FinishRegist = "Yes" });
        }

        [HttpPost]
        public IActionResult AjaxRegist(string username,string password,string phone_number,string email, string appkey)
        {
            var aService = Startup.ServicesProvider.GetBahamutAccountService();
            if (aService.AccountExists(username))
            {
                return Json(new { suc = false, msg = "user name already exists" });
            }
            var accountId = aService.AddAccount(new Account()
            {
                AccountName = username,
                CreateTime = DateTime.Now,
                Email = email,
                Mobile = phone_number,
                Password = password
            });
            return Json(new { suc = true, accountId = accountId });
        }

        [HttpGet]
        public string UserNameExists(string username)
        {
            var accountService = Startup.ServicesProvider.GetBahamutAccountService();
            if (accountService.AccountExists(username))
            {
                return "true";
            }
            else
            {
                return "false";
            }
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

        [HttpPost]
        public IActionResult AjaxLogin(string username,string password,string appkey)
        {
            var authService = Startup.ServicesProvider.GetAuthenticationService();
            try
            {
                var result = authService.LoginValidate(username, password);
                if (result.Succeeded)
                {
                    var svrCtrlService = Startup.ServicesProvider.GetServerControlManagementService();
                    var appInstance = svrCtrlService.GetMostFreeAppInstance(appkey);
                    var tokenService = Startup.ServicesProvider.GetTokenService();
                    var newSessionData = new AccountSessionData()
                    {
                        AccountId = result.AccountID,
                        Appkey = appkey
                    };
                    var atokenResult = tokenService.AllocateAccessToken(newSessionData).Result;
                    if (atokenResult == null)
                    {
                        throw new Exception("AllocateAccessToken Failed");
                    }
                    var parameters = new
                    {
                        LoginSuccessed = "true",
                        AccountID = result.AccountID,
                        AccessToken = atokenResult.AccessToken,
                        AppServerIP = appInstance.InstanceEndPointIP,
                        AppServerPort = appInstance.InstanceEndPointPort,
                        AppServiceUrl = appInstance.InstanceServiceUrl
                    };
                    return Json(parameters);
                }
            }
            catch (NoAppInstanceException)
            {
                return Json(new { msg = "No App Instance" });
            }
            catch (NullReferenceException ex)
            {
                return Json(new { msg = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { msg = ex.Message });
            }
            return Json(new {});
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
                        var appInstance = svrCtrlService.GetMostFreeAppInstance(appkey);
                        var tokenService = Startup.ServicesProvider.GetTokenService();
                        var newSessionData = new AccountSessionData()
                        {
                            AccountId = result.AccountID,
                            Appkey = appkey
                        };
                        var atokenResult = tokenService.AllocateAccessToken(newSessionData).Result;
                        if (atokenResult == null)
                        {
                            throw new Exception("AllocateAccessToken Failed");
                        }
                        var parameters = new
                        {
                            AccountID = result.AccountID,
                            AccessToken = atokenResult.AccessToken,
                            AppServerIP = appInstance.InstanceEndPointIP,
                            AppServerPort = appInstance.InstanceEndPointPort,
                            AppServiceUrl = appInstance.InstanceServiceUrl
                        };
                        return RedirectToAction("Returns", parameters);
                    }
                }catch(NoAppInstanceException)
                {
                    ModelState.AddModelError(string.Empty, "No App Service Online");
                    return PartialView(model);
                }
                catch (NullReferenceException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return PartialView(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    ModelState.AddModelError(string.Empty, "Server Error");
                    return PartialView(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return PartialView(model);
        }
    }
}
