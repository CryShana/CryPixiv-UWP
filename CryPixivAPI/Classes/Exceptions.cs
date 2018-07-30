using System;
using System.Collections.Generic;
using System.Text;

namespace CryPixivAPI.Classes
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message) { }
    }
    public class OffsetLimitException : Exception
    {
        public OffsetLimitException() : base() { }
    }
    public class EndReachedException : Exception
    {
        public EndReachedException() : base() { }
    }
}
