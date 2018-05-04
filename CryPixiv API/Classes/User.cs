using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryPixivAPI.Classes
{
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("mail_address")]
        public string MailAddress { get; set; }

        [JsonProperty("is_premium")]
        public bool IsPremium { get; set; }

        [JsonProperty("x_restrict")]
        public int XRestrict { get; set; }

        [JsonProperty("is_mail_authorized")]
        public bool IsMailAuthorized { get; set; }

        [JsonProperty("profile_image_urls")]
        public Dictionary<string,string> ProfileImageUrls { get; set; }
    }
}
