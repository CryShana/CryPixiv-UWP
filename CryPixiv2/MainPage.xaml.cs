﻿using CryPixiv2.Classes;
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
using Windows.System;
using Windows.UI;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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
            CoreWindow.GetForCurrentThread().KeyDown += KeyDownEventHandler;

            // Configure logger
            NLog.LogManager.Configuration.Variables["LogPath"] = LocalFolder.Path;
            Logger = NLog.LogManager.GetLogger("Main");

            // Upon closing app, make sure to properly flush logger
            Application.Current.Suspending += (a, b) => ViewModel.SaveData().Wait();
            Application.Current.UnhandledException += (a, b) =>
            {
                var ex = b.Exception;
                Logger.Fatal(ex, ex.Message + (ex.InnerException != null ? ex.InnerException.Message : ""));
            };

            // Set all necessary variables
            CurrentInstance = this;
            LocalStorage = ApplicationData.Current.LocalSettings;
            ViewModel = (MainViewModel)Application.Current.Resources["mainViewModel"];
            ViewModel.LoadData();

            // When user changes bookmarks - this will handle bookmarks being added or removed to/from the bookmark grid
            IllustrationGrid.IllustrationBookmarkChange += IllustrationGrid_IllustrationBookmarkChange;

            // When user clicks on any item in any grid - it will open the DetailsPage
            IllustrationGrid.ItemClicked += IllustrationGrid_ItemClicked;

            AttemptToLogin();
        }

        public async void AttemptToLogin()
        {
            // attempting to load previous data
            var deviceToken = LocalStorage.Values[Constants.StorageDeviceToken] as string;
            var refreshToken = LocalStorage.Values[Constants.StorageRefreshToken] as string;

            // initialize with device token (if not null, this will make sure to login with same device token)
            ViewModel.Account = new PixivAccount(deviceToken);

            // refresh authtoken using refreshtoken
            await ViewModel.Login(refreshToken);
        }

        #region IllustrationGrid Event Handlers
        public bool NavigatingToPage = false;
        public async void IllustrationGrid_ItemClicked(object sender, IllustrationWrapper e)
        {
            if (NavigatingToPage) return;
            NavigatingToPage = true;

            try
            {
                NavigateTo(typeof(DetailsPage),
                    new Tuple<PixivObservableCollection, IllustrationWrapper>(((IllustrationGrid)sender).ItemSource, e));
            }
            finally
            {
                // this is a quick solution to prevent double-clicking on item and navigating to same page twice
                await Task.Delay(500).ConfigureAwait(false);
                NavigatingToPage = false;
            }
        }

        void IllustrationGrid_IllustrationBookmarkChange(object sender, Tuple<IllustrationWrapper, bool> e)
        {
            if (e.Item1.IsBookmarked)
            {
                // Public
                if (e.Item2) ViewModel.BookmarksPublic.Insert(e.Item1);
                // Private
                else ViewModel.BookmarksPrivate.Insert(e.Item1);
            }
        }
        #endregion

        #region Download Switching
        // Download Manager handles which collections will be downloading at a time.
        // Right now download manager focuses on selected pivots only. Switches to them everytime selection changes.
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
        // SEARCH CLICK
        private async void _searchQuery_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter) return;

            var query = _searchQuery.Text;
            query = Regex.Replace(query, @"\s{2,}", " "); // remove unnecessary extra spaces

            if (query.Length == 0) return;
            _searchQuery.Text = "";

            var eq = ViewModel.Searches.Where(x => x.Query.Query == query).FirstOrDefault();
            if (eq != null)
            {
                // go to existing tab
                searchPivot.SelectedItem = eq;
                ShowNotification("Query already exists.");
                return;
            }

            // add new tab
            var sq = new SearchQuery() { Query = query };
            var q = new SearchSession(sq);
            ViewModel.Searches.Insert(0, q);

            // select new tab
            await Task.Delay(200);
            searchPivot.SelectedItem = q;

            // save search to history (remove oldest item if over limit)
            while (ViewModel.SearchHistory.Count >= Constants.MaximumSearchHistoryEntries) ViewModel.SearchHistory.RemoveAt(0);
            ViewModel.SearchHistory.Add(q.Query.Query);

            shouldScrollDown = true;
        }
        #endregion

        #region Login Buttons
        private void PasswordBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) LoginClick(null, null);
        }

        // This gets called when user clicks Login on the Login Form
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

        #region Navigation
        public void NavigateTo(Type pageType, object referencedObject) => Frame.Navigate(pageType, referencedObject, new DrillInNavigationTransitionInfo());
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }
        private void KeyDownEventHandler(CoreWindow sender, KeyEventArgs args) => HandleKey(args.VirtualKey);
        public void HandleKey(VirtualKey key)
        {
            var isBackPressed =
                key == VirtualKey.Escape ||
                key == VirtualKey.Back ||
                key == VirtualKey.GoBack;

            var el = FocusManager.GetFocusedElement();

            if (isBackPressed && el is TextBox == false) CurrentInstance.GoBack();
            else
            {
                var frame = Window.Current.Content as Frame;
                if (frame.Content is DetailsPage d)
                {
                    // if currently on details page
                    if (key == VirtualKey.E) d.NextIllustration();
                    else if (key == VirtualKey.Q) d.PreviousIllustration();
                }
            }
        }
        private void GoBack()
        {
            if (Frame.CanGoBack) Frame.GoBack();
        }
        #endregion

        #region Search Tab Context Menu
        private void CloseSearchTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((dynamic)sender).DataContext as SearchSession;
            ViewModel.Searches.Remove(item);
        }

        private void CopyTag_Click(object sender, RoutedEventArgs e)
        {
            var item = ((dynamic)sender).DataContext as SearchSession;
            GlobalFunctions.CopyToClipboardText(item.Query.Query);
            ShowNotification("Copied tag.");
        }

        private void ResetSearchTab_Click(object sender, RoutedEventArgs e)
        {
            var item = ((dynamic)sender).DataContext as SearchSession;
            item.Collection.Reset();

            DownloadManager.SwitchTo(item.Collection, ViewModel.Account);

            searchPivot.SelectedItem = item;

            ShowNotification("Collection reset.");
        }
        #endregion

        public void ShowNotification(string text, bool keepAlive = false)
        {
            Logger.Info("Notification shown: " + text);
            notification.Show(text, keepAlive ? 0 : Constants.InAppNotificationDuration);
        }

        #region Other Buttons
        private void PauseDownloadManagerClick(object sender, RoutedEventArgs e)
            => ViewModel.DownloadManagerPaused = !ViewModel.DownloadManagerPaused;

        private async void LogoutClick(object sender, RoutedEventArgs e)
        {
            // confirmation box
            var yesCommand = new UICommand("Yes", cmd => { });
            var noCommand = new UICommand("No", cmd => { });

            var dialog = new MessageDialog("Are you sure you wish to log out?", "Log out");
            dialog.Commands.Add(yesCommand);
            dialog.Commands.Add(noCommand);
            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 1;
            var command = await dialog.ShowAsync();

            if (command == yesCommand)
            {
                // clear login window data
                _password.Password = "";
                _username.Text = "";

                ViewModel.ClearAuthInfo();
                ViewModel.LoginFormShown = true;
            }
        }
        #endregion

        private void btnSettings_Click(object sender, RoutedEventArgs e)
            => NavigateTo(typeof(SettingsPage), null);

        #region Search History Button
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tag = _searchHistory.SelectedItem as string;
            if (tag == null) return;

            _searchQuery.Text = tag;
            _searchHistory.SelectedItem = null;
        }

        bool shouldScrollDown = false;
        private async void _searchHistory_DropDownOpened(object sender, object e)
        {
            if (shouldScrollDown == false) return;
            shouldScrollDown = false;

            await Task.Delay(200);
            var sw = _searchHistory.ScrollViewerComponent;
            var sh = _searchHistory.ScrollViewerComponent.ScrollableHeight;
            sw.ChangeView(null, sh, null);
        }

        private async void TextBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var textblock = (TextBlock)sender;
            var tag = (dynamic)textblock.DataContext as string;

            var txt = "";
            // take existing translation
            if (CurrentInstance.ViewModel.TranslatedWords.ContainsKey(tag)) txt = CurrentInstance.ViewModel.TranslatedWords[tag];
            else
            {
                // translate it
                txt = await Task.Run(() => Translator.Translate(tag));
                if (string.IsNullOrEmpty(txt) == false) CurrentInstance.ViewModel.TranslatedWords.TryAdd(tag, txt);
            }

            textblock.Text = txt;
        }
        #endregion
    }
}
