using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;

namespace SampleMvcApp.Controllers
{
    public class AccountController : Controller
    {
        IOptions<OpenIdConnectOptions> _options;

        public AccountController(IOptions<OpenIdConnectOptions> options)
        {
            _options = options;
        }

        public IActionResult Login(string returnUrl = null)
        {
            var lockContext = HttpContext.GenerateLockContext(_options.Value, returnUrl);

            return View(lockContext);
        }

        public IActionResult Logout()
        {
            HttpContext.Authentication.SignOutAsync("Auth0");
            HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }
    }
}
