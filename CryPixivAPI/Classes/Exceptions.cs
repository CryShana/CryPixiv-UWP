using System;
using System.Collections.Generic;
using System.Text;

namespace CryPixivAPI.Classes
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message) { }
    }
}
