using System;
using System.Security.Claims;
using Auth0.AuthenticationApi;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Options;
using SampleMvcApp.ViewModels;
using System.Threading.Tasks;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace SampleMvcApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly Auth0Settings _auth0Settings;

        public AccountController(IOptions<Auth0Settings> auth0Settings)
        {
            _auth0Settings = auth0Settings.Value;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    AuthenticationApiClient client = new AuthenticationApiClient(new Uri($"https://{_auth0Settings.Domain}/"));

                    var result = await client.AuthenticateAsync(new AuthenticationRequest
                    {
                        ClientId = _auth0Settings.ClientId,
                        Scope = "openid",
                        Connection = "Username-Password-Authentication", // Specify the correct name of your DB connection
                        Username = vm.EmailAddress,
                        Password = vm.Password
                    });

                    // Get user info from token
                    var user = await client.GetTokenInfoAsync(result.IdToken);

                    // Create claims principal
                    var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId), 
                        new Claim(ClaimTypes.Name, user.FullName)

                    }, CookieAuthenticationDefaults.AuthenticationScheme));

                    // Sign user into cookie middleware
                    await HttpContext.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    return RedirectToLocal(returnUrl);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult LoginExternal(string connection, string returnUrl = "/")
        {
            var properties = new AuthenticationProperties() { RedirectUri = returnUrl };

            if (!string.IsNullOrEmpty(connection))
                properties.Items.Add("connection", connection);

            return new ChallengeResult("Auth0", properties);
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

        #region Helpers

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
