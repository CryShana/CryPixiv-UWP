using CryPixiv2.Classes;
using CryPixiv2.Controls;
using CryPixiv2.ViewModels;
using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Microsoft.Identity.Client.Logger;

namespace CryPixiv2
{
    public sealed partial class MainPage : Page
    {
        public static MainPage CurrentInstance;
        public static NLog.Logger Logger;
        public static StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;     

        public MainViewModel ViewModel;
        public ApplicationDataContainer LocalStorage;
        public SystemNavigationManager NavigationManager;

        public MainPage()
        {
            this.InitializeComponent();

            // This is required for connected animations to properly work between different pages
            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.NavigationManager = SystemNavigationManager.GetForCurrentView();
            this.NavigationManager.BackRequested += (a, b) => GoBack();
            
            // Configure logger
            NLog.LogManager.Configuration.Variables["LogPath"] = LocalFolder.Path;
            Logger = NLog.LogManager.GetLogger("Main");

            // Upon closing app, make sure to properly flush logger
            Application.Current.Suspending += (a, b) => NLog.LogManager.Shutdown();
            Application.Current.UnhandledException += (a, b) => Logger.Fatal(b.Exception, b.Message);

            // Set all necessary variables
            CurrentInstance = this;
            LocalStorage = ApplicationData.Current.LocalSettings;
            ViewModel = (MainViewModel)Application.Current.Resources["mainViewModel"];

            // When user changes bookmarks - this will handle bookmarks being added or removed to/from the bookmark grid
            IllustrationGrid.IllustrationBookmarkChange += IllustrationGrid_IllustrationBookmarkChange;

            // When user clicks on any item in any grid - it will open the DetailsPage
            IllustrationGrid.ItemClicked += IllustrationGrid_ItemClicked;

            AttemptToLogin();
        }

        public async void AttemptToLogin()
        {
            var deviceToken = LocalStorage.Values[Constants.StorageDeviceToken] as string;
            var refreshToken = LocalStorage.Values[Constants.StorageRefreshToken] as string;

            ViewModel.Account = new PixivAccount(deviceToken);
            await ViewModel.Login(refreshToken);
        }

        #region IllustrationGrid Event Handlers
        void IllustrationGrid_ItemClicked(object sender, IllustrationWrapper e) => NavigateTo(typeof(DetailsPage), e);
        void IllustrationGrid_IllustrationBookmarkChange(object sender, Tuple<IllustrationWrapper, bool> e)
        {
            if (e.Item1.IsBookmarked)
            {
                if (e.Item2)
                {
                    ViewModel.BookmarksPublic.Insert(e.Item1);

                    if (ViewModel.BookmarksPublic.LoadedElements.ContainsKey(e.Item1.WrappedIllustration.Id) == false)
                        ViewModel.BookmarksPublic.LoadedElements.Add(e.Item1.WrappedIllustration.Id, DateTime.Now);
                }
                else
                {
                    ViewModel.BookmarksPrivate.Insert(e.Item1);

                    if (ViewModel.BookmarksPrivate.LoadedElements.ContainsKey(e.Item1.WrappedIllustration.Id) == false)
                        ViewModel.BookmarksPrivate.LoadedElements.Add(e.Item1.WrappedIllustration.Id, DateTime.Now);
                }
            }
        } 
        #endregion

        #region Download Switching
        private void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (Pivot)sender;
            var pivotItem = pivot.SelectedItem as PivotItem;
            if (pivotItem == null) return;

            var header = pivotItem.Header.ToString().ToLower();
            switch (header)
            {
                case "search":
                    searchPivot_SelectionChanged(searchPivot, null);
                    break;
                case "ranking":
                    rankingPivot_SelectionChanged(rankingPivot, null);
                    break;
                case "following":
                    followingPivot_SelectionChanged(followingPivot, null);
                    break;
                case "recommended":
                    DownloadManager.SwitchTo(ViewModel.Recommended, ViewModel.Account);
                    break;
                case "bookmarks":
                    bookmarksPivot_SelectionChanged(bookmarksPivot, null);
                    break;
                default:
                    throw new NotImplementedException("This should not happen. Please check pivot item headers!");
            }
        }
        private void rankingPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (Pivot)sender;
            var pivotItem = pivot.SelectedItem as PivotItem;
            if (pivotItem == null) return;

            var header = pivotItem.Header.ToString().ToLower().Replace(" ", "");
            switch (header)
            {
                case "daily":
                    DownloadManager.SwitchTo(ViewModel.RankingDaily, ViewModel.Account);
                    break;
                case "weekly":
                    DownloadManager.SwitchTo(ViewModel.RankingWeekly, ViewModel.Account);
                    break;
                case "monthly":
                    DownloadManager.SwitchTo(ViewModel.RankingMonthly, ViewModel.Account);
                    break;
                case "daily(male)":
                    DownloadManager.SwitchTo(ViewModel.RankingDailyMale, ViewModel.Account);
                    break;
                case "daily(female)":
                    DownloadManager.SwitchTo(ViewModel.RankingDailyFemale, ViewModel.Account);
                    break;
                case "dailyr18":
                    DownloadManager.SwitchTo(ViewModel.RankingDaily18, ViewModel.Account);
                    break;
                case "weeklyr18":
                    DownloadManager.SwitchTo(ViewModel.RankingWeekly18, ViewModel.Account);
                    break;
                case "dailyr18(male)":
                    DownloadManager.SwitchTo(ViewModel.RankingDailyMale18, ViewModel.Account);
                    break;
                case "dailyr18(female)":
                    DownloadManager.SwitchTo(ViewModel.RankingDailyFemale18, ViewModel.Account);
                    break;
                default:
                    throw new NotImplementedException("This should not happen. Please check pivot item headers!");
            }
        }
        private void bookmarksPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (Pivot)sender;
            var pivotItem = pivot.SelectedItem as PivotItem;
            if (pivotItem == null) return;

            var header = pivotItem.Header.ToString().ToLower();
            switch (header)
            {
                case "public":
                    DownloadManager.SwitchTo(ViewModel.BookmarksPublic, ViewModel.Account);
                    break;
                case "private":
                    DownloadManager.SwitchTo(ViewModel.BookmarksPrivate, ViewModel.Account);
                    break;
                default:
                    throw new NotImplementedException("This should not happen. Please check pivot item headers!");
            }
        }
        private void followingPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (Pivot)sender;
            var pivotItem = pivot.SelectedItem as PivotItem;
            if (pivotItem == null) return;

            var header = pivotItem.Header.ToString().ToLower();
            switch (header)
            {
                case "public":
                    DownloadManager.SwitchTo(ViewModel.FollowingPublic, ViewModel.Account);
                    break;
                case "private":
                    DownloadManager.SwitchTo(ViewModel.FollowingPrivate, ViewModel.Account);
                    break;
                default:
                    throw new NotImplementedException("This should not happen. Please check pivot item headers!");
            }
        }
        private void searchPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pivot = (Pivot)sender;
            var session = pivot.SelectedItem as SearchSession;
            if (session == null) return;

            DownloadManager.SwitchTo(session.Collection, ViewModel.Account);
        }
        #endregion

        #region Search
        private async void _searchQuery_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;

            var query = _searchQuery.Text;
            query = Regex.Replace(query, @"\s{2,}", " "); // remove unnecessary extra spaces

            if (query.Length == 0) return;
            if (ViewModel.Searches.Count(x => x.Query.Query == query) > 0)
            {
                new MessageDialog("Specified query already exists! Check existing tabs.", "Query exists").ShowAsync();
                return;
            }

            // add new tab
            var sq = new SearchQuery() { Query = query };
            var q = new SearchSession(sq);
            ViewModel.Searches.Add(q);
            _searchQuery.Text = "";

            // select new tab
            await Task.Delay(200);
            searchPivot.SelectedItem = q;
        } 
        #endregion

        #region Login Buttons
        private void PasswordBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) LoginClick(null, null);
        }
        private async void LoginClick(object sender, RoutedEventArgs e)
        {
            var username = _username.Text;
            var password = _password.Password;
            if (username.Length == 0 || password.Length == 0)
            {
                ViewModel.LoginFormErrorMessage = "Invalid username or password!";
                return;
            }

            await ViewModel.Login(username, password);
            if (ViewModel.LoginFormShown) _username.Focus(FocusState.Keyboard);
        } 
        #endregion

        public void NavigateTo(Type pageType, object referencedObject) => Frame.Navigate(pageType, referencedObject);
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        public void GoBack()
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }
    }
}
