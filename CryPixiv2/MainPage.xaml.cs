using CryPixiv2.Classes;
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
           
            DoStuff();
        }


        public async void DoStuff()
        {
            ViewModel.Account = new PixivAccount("fa2226814b46768e9f0ea3aafac61eb6");
            await ViewModel.Account.Login("IuEsI8_15UjDFtSfaOcqJkPCK3oe12IzQDMwP4mz_qA");          
            
            /*
            var ill = await ViewModel.Account.GetBookmarks();
            addStuff(ill);

            async void addStuff(IllustrationResponse r)
            {
                foreach (var l in r.Illustrations) ViewModel.BookmarksPublic.Add(new IllustrationWrapper(l, ViewModel.Account));

                try
                {
                    var np = await r.NextPage();
                    addStuff(np);
                }
                catch { }
            }    */
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
                    // switch to Searching
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
                case "dailyr18male":
                    DownloadManager.SwitchTo(ViewModel.RankingDailyMale18, ViewModel.Account);
                    break;
                case "dailyr18female":
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
        #endregion
    }
}
