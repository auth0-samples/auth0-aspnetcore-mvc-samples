using System;
using System.Collections.Generic;

namespace Auth0.ASPNETCore.MVC
{
    public class Auth0AuthorizeOptions
    {
        public string Audience { get; set; }
        public string Organization { get; set; }
        public Dictionary<string, string> ExtraParameters { get; set; }
    }
    public class Auth0Options
    {
        public string Domain { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public ICollection<String> Scope { get; set; }
        public bool UseRefreshTokens { get; set; }
        public string CallbackPath { get; set; }
        public Auth0AuthorizeOptions AuthorizeOptions { get; set; }


    }
}
