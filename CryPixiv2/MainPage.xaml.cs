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
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace CryPixiv2
{
    public sealed partial class MainPage : Page
    {
        public static MainPage CurrentInstance;
        public MainViewModel ViewModel;

        public MainPage()
        {
            this.InitializeComponent();

            CurrentInstance = this;
            ViewModel = (MainViewModel)Application.Current.Resources["mainViewModel"];
            IllustrationGrid.IllustrationBookmarkChange += IllustrationGrid_IllustrationBookmarkChange;
           
            DoStuff();
        }

        void IllustrationGrid_IllustrationBookmarkChange(object sender, Tuple<IllustrationWrapper, bool> e)
        {
            if (e.Item1.IsBookmarked)
            {
                if (e.Item2) ViewModel.BookmarksPublic.Insert(e.Item1);
                else ViewModel.BookmarksPrivate.Insert(e.Item1);
            }
        }

        public async void DoStuff()
        {
            ViewModel.Account = new PixivAccount("fa2226814b46768e9f0ea3aafac61eb6");
            await ViewModel.Account.Login("IuEsI8_15UjDFtSfaOcqJkPCK3oe12IzQDMwP4mz_qA");              
        }

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

        private void collection_IllustrationBookmarkChange(object sender, IllustrationWrapper e)
        {

        }
    }
}
