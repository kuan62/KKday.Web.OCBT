using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KKday.Web.OCBT.AppCode;
using KKday.Web.OCBT.Models.Model.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KKday.Web.OCBT.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "KKdayOnly")]
    public class LoginController : Controller
    {
        // GET: /<controller>/
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Claim取使用者類型
            var userType = User.FindFirst("Account")?.Value;
            if (userType != null) return Redirect("~/");

            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Authen(LoginRqModel rq)
        {
            string guidKey = Guid.NewGuid().ToString();
            Dictionary<string, string> jsonData = new Dictionary<string, string>();

            try
            {
                var claims = new List<Claim>
                {
                    new Claim("Account", rq.email),
                    new Claim("GuidKey", guidKey),
                    new Claim("IdentityType", "KKDAY"),
                    new Claim("Ver", Website.Instance.PrincipleVersion), // 帶入當前ClaimPriciple版號
                };
                var userIdentity = new ClaimsIdentity(claims, "login");
                ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties()
                    {
                        ExpiresUtc = DateTime.UtcNow.AddDays(10), // 預設 Cookie 有效時間
                        IsPersistent = false,
                        AllowRefresh = true
                    });
                jsonData.Add("status", "OK");
                jsonData.Add("GuidKey", guidKey);
                jsonData.Add("url", Url.Content("~/"));

                #region 登入後重新設定使用者的 Cookie Culture

                // KKday-Locale 轉換對應到 RFC 4646 
                var _culture = "zh-TW";//LocaleTool.Convert(account.LOCALE);

                // 設定 Culture
                HttpContext.Response.Cookies.Append(
                      CookieRequestCultureProvider.DefaultCookieName,
                      CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(_culture))
                   );

                #endregion 登入後重新設定使用者的 Cookie Culture
            }
            catch (Exception ex)
            {
                jsonData.Clear();
                jsonData.Add("status", "ERROR");
                jsonData.Add("msg", ex.Message);
            }
            return Json(jsonData);
        }

        // 使用者登出 
        [AllowAnonymous]
        public async Task<IActionResult> LogOutAsync()
        {
            try
            {
                var account = User.FindFirstValue("Account")?.ToString();
                //Website.Instance.logger.Info($"Logout_AuthenAsync_Account:{JsonConvert.SerializeObject(account)}");
                await HttpContext.SignOutAsync();
            }
            catch (Exception ex)
            {
                await HttpContext.SignOutAsync();
            }

            return Redirect("~/");
        }
    }
}
