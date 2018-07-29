using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryPixiv2
{
    public static class Constants
    {
        public const string StorageDeviceToken = "deviceToken";
        public const string StorageRefreshToken = "refreshToken";

        public const string ConnectedAnimationThumbnail = "ca1";
        public const string ConnectedAnimationImage = "ca2";

        public const double TimeTillAnimationSkipMs = 1000;
        public const double ItemGridEntryAnimationDurationMs = 900;
        public const int InAppNotificationDuration = 1200;
    }
}
