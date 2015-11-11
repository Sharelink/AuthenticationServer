using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using AuthenticationServer.Models;
using BahamutService;
using BahamutService.Model;
using ServerControlService.Service;
using System;
using BahamutCommon;
using NLog;

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

        [HttpPost]
        public IActionResult AjaxRegist(string username,string password,string phone_number,string email, string appkey)
        {
            var aService = Startup.ServicesProvider.GetBahamutAccountService();
            if (aService.AccountExists(username))
            {
                LogManager.GetCurrentClassLogger().Warn("AjaxRegist:UserNameExists:{0}", username);
                return Json(new { suc = false, msg = "USER_NAME_EXISTS" });
            }
            var accountId = aService.AddAccount(new Account()
            {
                AccountName = username,
                CreateTime = DateTime.UtcNow,
                Email = email,
                Mobile = phone_number,
                Password = password
            });
            LogManager.GetCurrentClassLogger().Info("AjaxRegist:{0}", username);
            return Json(new { suc = true, accountId = accountId, accountName = username });
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

        [HttpPost]
        public async Task<IActionResult> AjaxLogin(string username, string password, string appkey)
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
                    var atokenResult = await tokenService.AllocateAccessToken(newSessionData);
                    if (atokenResult == null)
                    {
                        LogManager.GetCurrentClassLogger().Warn("AjaxLogin:Allocate Access Token Failed");
                        return Json(new { msg = "ALLOC_TOKEN_FAILED" });
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
            catch (NoAppInstanceException ex)
            {
                LogManager.GetCurrentClassLogger().Warn(ex, "AjaxLogin:No App Server Instance");
                return Json(new { msg = "NO_APP_INSTANCE" });
            }
            catch (NullReferenceException ex)
            {
                LogManager.GetCurrentClassLogger().Warn(ex, "AjaxLogin");
                return Json(new { msg = ex.Message });
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "AjaxLogin:Server Error");
                return Json(new { msg = "SERVER_ERROR" });
            }
            LogManager.GetCurrentClassLogger().Error("AjaxLogin:Server Error");
            return Json(new { msg = "SERVER_ERROR" });
        }
    }
}
