using System;
using System.Collections.Generic;

namespace Auth0.ASPNETCore.WebApi
{
    public class Auth0Options
    {
        public string Domain { get; set; }
        public string Audience { get; set; }
        public ICollection<String> Scopes { get; set; } = new List<string>();
    }
}
