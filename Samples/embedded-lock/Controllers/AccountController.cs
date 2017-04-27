using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;

namespace SampleMvcApp.Controllers
{
    public class AccountController : Controller
    {
        IOptions<OpenIdConnectOptions> _options;

        public AccountController(IOptions<OpenIdConnectOptions> options)
        {
            _options = options;
        }

        public IActionResult Login(string returnUrl = "/")
        {
            var lockContext = HttpContext.GenerateLockContext(_options.Value, returnUrl);

            return View(lockContext);
        }

        [Authorize]
        public IActionResult Logout()
        {
            HttpContext.Authentication.SignOutAsync("Auth0");
            HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// This is just a helper action to enable you to easily see all claims related to a user. It helps when debugging your
        /// application to see the in claims populated from the Auth0 ID Token
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public IActionResult Claims()
        {
            return View();
        }
    }
}
