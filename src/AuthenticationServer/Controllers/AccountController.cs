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

        [HttpPost]
        public IActionResult AjaxRegist(string username,string password,string phone_number,string email, string appkey)
        {
            var aService = Startup.ServicesProvider.GetBahamutAccountService();
            try
            {
                if (aService.AccountExists(username))
                {
                    LogManager.GetCurrentClassLogger().Warn("AjaxRegist:UserNameExists:{0}", username);
                    return Json(new { suc = false, msg = "USER_NAME_EXISTS" });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                LogManager.GetCurrentClassLogger().Error(ex, "AjaxRegist:Server Error");
                return Json(new { suc = false, msg = "SERVER_ERROR" });
            }
            
            var accountId = aService.AddAccount(new Account()
            {
                AccountName = username,
                CreateTime = DateTime.UtcNow,
                Email = email,
                Mobile = phone_number,
                Password = password
            });
            LogManager.GetLogger("Info").Info("AjaxRegist:{0}", username);
            return Json(new { suc = true, accountId = accountId, accountName = username });
        }

        [HttpGet]
        public string UserNameExists(string username)
        {
            var accountService = Startup.ServicesProvider.GetBahamutAccountService();
            try
            {
                if (accountService.AccountExists(username))
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                LogManager.GetCurrentClassLogger().Error(ex, "UserNameExists:Server Error");
                return "SERVER_ERROR";
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
                        AccountName = result.AccountName,
                        BindMobile = result.ValidatedMobile,
                        BindEmail = result.ValidatedEmail,
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
                LogManager.GetLogger("Main").Error(ex, "AjaxLogin:No App Server Instance");
                return Json(new { msg = "NO_APP_INSTANCE" });
            }
            catch (NullReferenceException ex)
            {
                LogManager.GetCurrentClassLogger().Warn(ex, "AjaxLogin->{0}", ex.Message);
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
