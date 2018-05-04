using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CryPixiv_API.Classes
{
    public class ErrorResponse
    {
        [JsonProperty("has_error")]
        public bool HasError { get; set; }

        [JsonProperty("errors")]
        public ErrorsObject Errors { get; set; }
    }

    public class ErrorsObject
    {
        [JsonProperty("system")]
        public SystemObject System { get; set; }
    }

    public class SystemObject
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
