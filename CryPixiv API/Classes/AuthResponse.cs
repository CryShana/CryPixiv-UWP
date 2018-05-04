using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CryPixiv_API.Classes
{
    
    public class AuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("device_token")]
        public string DeviceToken { get; set; }

        [JsonProperty("user")]
        public User User { get; set; } 
    }
}
