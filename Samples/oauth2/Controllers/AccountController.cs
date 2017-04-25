using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspNetCoreOAuth2Sample.Controllers
{
    public class AccountController: Controller
    {
        private readonly IOptions<Auth0Settings> _auth0Settings;

        public AccountController(IOptions<Auth0Settings> auth0Settings)
        {
            _auth0Settings = auth0Settings;
        }

        public IActionResult Login(string returnUrl = "/")
        {
            return new ChallengeResult("Auth0", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        [Authorize]
        public async Task Logout()
        {
            // Sign the user out of the cookie authentication middleware (i.e. it will clear the local session cookie)
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Construct the post-logout URL (i.e. where we'll tell Auth0 to redirect after logging the user out)
            var request = HttpContext.Request;
            string postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + Url.Action("Index", "Home");

            // Redirect to the Auth0 logout endpoint in order to log out of Auth0
            string logoutUri = $"https://{_auth0Settings.Value.Domain}/v2/logout?client_id={_auth0Settings.Value.ClientId}&returnTo={Uri.EscapeDataString(postLogoutUri)}";
            HttpContext.Response.Redirect(logoutUri);
        }

        [Authorize]
        public IActionResult UserProfile()
        {
            return View();
        }
    }
}