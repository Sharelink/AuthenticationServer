using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Net;
using BahamutCommon;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AuthenticationServer.Controllers
{
    [Route("[controller]")]
    public class BahamutAccountsController : Controller
    {
        // GET /Accounts : return my account information
        [HttpGet]
        public async Task<object> Get(string appkey, string appToken, string accountId, string userId)
        {
            var tokenService = Startup.ServicesProvider.GetTokenService();
            var result = await tokenService.ValidateAppToken(appkey, userId, appToken);
            if (result != null && result.AccountId == accountId)
            {
                var accountService = Startup.ServicesProvider.GetBahamutAccountService();
                var account = accountService.GetAccount(accountId);
                return new
                {
                    accountId = account.AccountID,
                    accountName = account.AccountName,
                    createTime = DateTimeUtil.ToString(account.CreateTime),
                    name = account.Name,
                    mobile = account.Mobile,
                    email = account.Email
                };
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.NotModified;
                return new { msg = "TOKEN_UNAUTHORIZED" };
            }
        }

        // PUT /Accounts/Name (name) : update my account name properties
        [HttpPut("Name")]
        public async Task PutName(string appkey, string appToken, string accountId, string userId,string name)
        {
            var tokenService = Startup.ServicesProvider.GetTokenService();
            var result = await tokenService.ValidateAppToken(appkey, userId, appToken);
            if (result != null && result.AccountId == accountId)
            {
                var accountService = Startup.ServicesProvider.GetBahamutAccountService();
                if (name != null)
                {
                    if (!accountService.ChangeName(accountId, name))
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotModified;
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.NotModified;
                }
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.NotModified;
            }
        }

        // PUT /Accounts/Name (name) : update my account birth properties
        [HttpPut("BirthDate")]
        public async Task PutBirthDate(string appkey, string appToken, string accountId, string userId,string birthdate)
        {
            var tokenService = Startup.ServicesProvider.GetTokenService();
            var result = await tokenService.ValidateAppToken(appkey, userId, appToken);
            if (result != null && result.AccountId == accountId)
            {
                var accountService = Startup.ServicesProvider.GetBahamutAccountService();
                if (birthdate != null)
                {
                    if (!accountService.ChangeAccountBirthday(accountId, DateTimeUtil.ToDate(birthdate)))
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotModified;
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.NotModified;
                }
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.NotModified;
            }
        }

        [HttpPut("Password")]
        public async Task<object> ChangePassword(string appkey, string appToken,string accountId, string userId, string originPassword, string newPassword)
        {
            var tokenService = Startup.ServicesProvider.GetTokenService();
            var result = await tokenService.ValidateAppToken(appkey, userId, appToken);
            if (result != null && result.AccountId == accountId)
            {
                var accountService = Startup.ServicesProvider.GetBahamutAccountService();
                var suc = accountService.ChangePassword(accountId, originPassword, newPassword);
                if (suc == false)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotModified;
                    return new { msg = "CHANGE_PASSWORD_ERROR" };
                }
                return new { msg = "CHANGE_PASSWORD_SUCCESS" };
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return new { msg = "TOKEN_UNAUTHORIZED" };
            }
        }
    }
}
