using CryPixiv2.Classes;
using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;

namespace CryPixiv2.ViewModels
{
    public class MainViewModel : Notifier
    {
        #region Private Fields
        private PixivObservableCollection
            bookmarksPublic = new PixivObservableCollection(a => a.GetBookmarks(true)),
            bookmarksPrivate = new PixivObservableCollection(a => a.GetBookmarks(false)),
            recommended = new PixivObservableCollection(a => a.GetRecommended()),
            followingPublic = new PixivObservableCollection(a => a.GetNewestFollowingWorks(true)),
            followingPrivate = new PixivObservableCollection(a => a.GetNewestFollowingWorks(false)),
            rankingDaily = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily)),
            rankingWeekly = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Weekly)),
            rankingMonthly = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Monthly)),
            rankingDailyMale = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Male)),
            rankingDailyFemale = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Female)),
            rankingDaily18 = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_R18)),
            rankingWeekly18 = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Weekly_R18)),
            rankingDailyMale18 = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Male_R18)),
            rankingDailyFemale18 = new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Female_R18));

        private ObservableCollection<SearchSession> searches = new ObservableCollection<SearchSession>();
        private bool isloggingin = false, loginform = false;
        private string loginerror = "";
        private int blacklistedCount = 0;
        #endregion

        #region Public Properties
        public PixivAccount Account { get; set; }

        #region Allow Levels
        int allowlvl = 2;
        public int AllowLevel
        {
            get => allowlvl;
            set
            {
                allowlvl = value;

                // so that IllustrationWrappers can notice it and update IsBlurred property
                AllowLevelChanged?.Invoke(this, allowlvl);  

                // update the slider values on every illustration grid
                Changed();
            }
        } 
        #endregion
        public PixivObservableCollection BookmarksPublic { get => bookmarksPublic; set { bookmarksPublic = value; Changed(); } }
        public PixivObservableCollection BookmarksPrivate { get => bookmarksPrivate; set { bookmarksPrivate = value; Changed(); } }
        public PixivObservableCollection Recommended { get => recommended; set { recommended = value; Changed(); } }
        public PixivObservableCollection FollowingPublic { get => followingPublic; set { followingPublic = value; Changed(); } }
        public PixivObservableCollection FollowingPrivate { get => followingPrivate; set { followingPrivate = value; Changed(); } }
        public PixivObservableCollection RankingDaily { get => rankingDaily; set { rankingDaily = value; Changed(); } }
        public PixivObservableCollection RankingWeekly { get => rankingWeekly; set { rankingWeekly = value; Changed(); } }
        public PixivObservableCollection RankingMonthly { get => rankingMonthly; set { rankingMonthly = value; Changed(); } }
        public PixivObservableCollection RankingDailyMale { get => rankingDailyMale; set { rankingDailyMale = value; Changed(); } }
        public PixivObservableCollection RankingDailyFemale { get => rankingDailyFemale; set { rankingDailyFemale = value; Changed(); } }
        public PixivObservableCollection RankingDaily18 { get => rankingDaily18; set { rankingDaily18 = value; Changed(); } }
        public PixivObservableCollection RankingWeekly18 { get => rankingWeekly18; set { rankingWeekly18 = value; Changed(); } }
        public PixivObservableCollection RankingDailyMale18 { get => rankingDailyMale18; set { rankingDailyMale18 = value; Changed(); } }
        public PixivObservableCollection RankingDailyFemale18 { get => rankingDailyFemale18; set { rankingDailyFemale18 = value; Changed(); } }
        public ObservableCollection<SearchSession> Searches { get => searches; set { searches = value; Changed(); } }       

        #region Data
        // Any translated words should be cached here for later use - this will not only speed up translation loading, but also not waste network 
        public ConcurrentDictionary<string, string> TranslatedWords { get; set; }
        public List<string> SearchHistory { get; set; }
        public HashSet<int> BlockedIllustrations { get; set; }
        public List<string> BlacklistedTags { get; set; }
        public PageAction PageAction_DetailsImageDoubleClick { get; set; }

        // values of enum values must reflect indexes in Settings page combobox
        [Serializable]
        public enum PageAction
        {
            ToggleFullscreen = 0,
            NavigateBack = 1,
            CopyImage = 2,
            GoToNextIllustration = 3
        }
        #endregion

        public bool IsLoggingIn { get => isloggingin; set { isloggingin = value; Changed(); } }
        public bool LoginFormShown { get => loginform; set { loginform = value; Changed(); } }
        public string LoginFormErrorMessage { get => loginerror; set { loginerror = value; Changed(); } }
        public bool DownloadManagerPaused
        {
            get => DownloadManager.IsPaused;
            set
            {
                DownloadManager.IsPaused = value;
                Changed();
            }
        }
        public int BlacklistedCount { get => blacklistedCount; set { blacklistedCount = value; Changed(); } }
        public event EventHandler<int> AllowLevelChanged;
        #endregion

        public MainViewModel()
        {
            DownloadManager.BlacklistedIllustration += (a, b) => BlacklistedCount++;
        }

        private void SaveAuthInfo()
        {
            var ls = MainPage.CurrentInstance.LocalStorage;

            ls.Values[Constants.StorageDeviceToken] = Account.AuthInfo.DeviceToken;
            ls.Values[Constants.StorageRefreshToken] = Account.AuthInfo.RefreshToken;
        }

        public void ClearAuthInfo()
        {
            var ls = MainPage.CurrentInstance.LocalStorage;

            ls.Values[Constants.StorageDeviceToken] = null;
            ls.Values[Constants.StorageRefreshToken] = null;
        }

        /// <summary>
        /// Load Translations, Search History, Blocked Illustrations
        /// </summary>
        public async Task LoadData()
        {
            // try to get save file
            StorageFile savefile = null;
            try
            {
                savefile = await MainPage.LocalFolder.GetFileAsync(Constants.SaveFileName);
            }
            catch (Exception ex)
            {
                MainPage.Logger.Info($"Savefile not found: {ex.Message}");
            }

            // try to deserialize it if save file exists
            SaveFile save = null;
            try
            {
                if (savefile == null) throw new NullReferenceException();

                var data = (await FileIO.ReadBufferAsync(savefile)).ToArray();
                save = GlobalFunctions.Deserialize<SaveFile>(data);
            }
            catch
            {
                SearchHistory = new List<string>();
                BlacklistedTags = new List<string>();
                BlockedIllustrations = new HashSet<int>();
                TranslatedWords = new ConcurrentDictionary<string, string>();
                PageAction_DetailsImageDoubleClick = Constants.DefaultPageAction_DetailsImageDoubleClick;
                return;
            }

            // translated words
            TranslatedWords = new ConcurrentDictionary<string, string>();
            var tlist = save.TranslatedWords;
            foreach (var p in tlist) TranslatedWords.TryAdd(p.Key, p.Value);

            // search history
            SearchHistory = save.SearchHistory ?? new List<string>();

            // blocked illustrations
            BlockedIllustrations = save.BlockedIllustrations ?? new HashSet<int>();

            // blacklisted tags
            BlacklistedTags = save.BlacklistedTags ?? new List<string>();

            // actions
            PageAction_DetailsImageDoubleClick = save.PageAction_DetailsImageDoubleClick;
        }

        /// <summary>
        /// Save Translations, Search History, Blocked Illustrations
        /// </summary>
        public async Task SaveData()
        {
            // check if data wasn't loaded - then you should not save
            if (TranslatedWords == null || SearchHistory == null || BlockedIllustrations == null) return;

            SaveFile save = new SaveFile()
            {
                TranslatedWords = TranslatedWords?.ToList(),
                SearchHistory = SearchHistory,
                BlockedIllustrations = BlockedIllustrations,
                BlacklistedTags = BlacklistedTags,
                PageAction_DetailsImageDoubleClick = PageAction_DetailsImageDoubleClick
            };

            var savefile = await (MainPage.LocalFolder.CreateFileAsync(Constants.SaveFileName, CreationCollisionOption.ReplaceExisting).AsTask()).ConfigureAwait(false);
            await (FileIO.WriteBytesAsync(savefile, GlobalFunctions.Serialize(save)).AsTask()).ConfigureAwait(false);
        }

        public async Task Login(string username, string password) => await Login(username, password, null);
        public async Task Login(string refreshToken) => await Login(null, null, refreshToken);
        private async Task Login(string username, string password, string refreshToken)
        {
            try
            {
                MainPage.Logger.Info($"Attempting to login... " +
                    $"(Parameters: {(username == null ? "null" : username)}, " +
                    $"{(password == null ? "null" : "Password given")}, " +
                    $"{(refreshToken == null ? "null" : "Refresh token given")})");

                if (string.IsNullOrEmpty(refreshToken) &&
                    (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
                    throw new LoginException("User not logged in!");

                IsLoggingIn = true;

                // use refresh token if present, otherwise show login form
                if (!string.IsNullOrEmpty(refreshToken)) await Account.Login(refreshToken);
                else
                {
                    LoginFormShown = false;
                    LoginFormErrorMessage = "";
                    await Account.Login(username, password);
                }

                SaveAuthInfo();
                MainPage.Logger.Info($"Logged in successfully.");
            }
            catch (LoginException lex)
            {
                // failed to login (either 'invalid refresh token' or 'user not logged in')
                LoginFormShown = true;
                LoginFormErrorMessage = lex.Message;
                MainPage.Logger.Warn(lex.Message);
            }
            catch (HttpRequestException httpex)
            {
                // internet error - set timeout and try again later (add this later)
                LoginFormShown = true;
                LoginFormErrorMessage = httpex.Message;

                var exvar = httpex;
                MainPage.Logger.Error(exvar, "HTTP error while trying to login!");
            }
            catch (Exception ex)
            {
                // unkown error - set timeout and try again later (add this later)
                LoginFormShown = true;
                LoginFormErrorMessage = ex.Message;

                var exvar = ex;
                MainPage.Logger.Error(exvar, "Unknown error while trying to login!");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
    }
}
