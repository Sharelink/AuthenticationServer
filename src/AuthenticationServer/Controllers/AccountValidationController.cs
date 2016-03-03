using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Net;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AuthenticationServer.Controllers
{
    public class AccountValidationController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string appkey, string accessToken,string accountId, string userId, string originPassword, string newPassword)
        {
            var tokenService = Startup.ServicesProvider.GetTokenService();
            var result = await tokenService.ValidateAppToken(appkey, userId, accessToken);
            if (result != null)
            {

                var accountService = Startup.ServicesProvider.GetBahamutAccountService();
                var suc = accountService.ChangePassword(accountId, originPassword, newPassword);
                if (suc == false)
                {
                    Response.StatusCode = (int)HttpStatusCode.NotModified;
                    return Json(new { msg = "CHANGE_PASSWORD_ERROR" });
                }
                return Json(new { msg = "CHANGE_PASSWORD_SUCCESS" });
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { msg = "TOKEN_UNAUTHORIZED" });
            }
        }
    }
}
