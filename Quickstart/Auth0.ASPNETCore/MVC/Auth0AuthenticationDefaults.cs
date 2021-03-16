using Microsoft.AspNetCore.Authentication.Cookies;

namespace Auth0.ASPNETCore.MVC
{
    public class Auth0AuthenticationDefaults
    {
        public static string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }
}
