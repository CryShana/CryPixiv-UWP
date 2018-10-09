using static CryPixiv2.ViewModels.MainViewModel;

namespace CryPixiv2
{
    public static class Constants
    {
        public const string StorageDeviceToken = "deviceToken";
        public const string StorageRefreshToken = "refreshToken";
        public const string StorageTranslations = "translations";
        public const string StorageHistory = "searchHistory";
        public const string StorageBlockedIllustrations = "blockedIllustrations";

        public const string ConnectedAnimationThumbnail = "ca1";
        public const string ConnectedAnimationImage = "ca2";
        public const string ConnectedAnimationArtist = "ca3";

        public const double TimeTillAnimationSkipMs = 1000;
        public const double ItemGridEntryAnimationDurationMs = 900;
        public const int InAppNotificationDuration = 1200;

        public const int MaximumSearchHistoryEntries = 60;
        public const double ImageTransitionDuration = 0.3;
        public const double ArtistImageTransitionDuration = 0.5;

        public const string SaveFileName = "savedata.data";

        public const PageAction DefaultPageAction_DetailsImageDoubleClick = PageAction.ToggleFullscreen;
    }
}
