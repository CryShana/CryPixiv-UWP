﻿using CryPixiv2.Classes;
using CryPixiv2.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CryPixiv2
{
    public sealed partial class ArtistPage : Page, INotifyPropertyChanged
    {
        #region Stuff
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed([CallerMemberName]string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public long ArtistId { get; set; } = 0;
        IllustrationWrapper illust = null;
        PixivObservableCollection artistWorks, prevCollection;
        #endregion

        public static ArtistPage CurrentInstance;
        public PixivObservableCollection ArtistWorks { get => artistWorks; set { artistWorks = value; Changed(); } }

        public ArtistPage()
        {
            this.InitializeComponent();
            this.PointerPressed += DetailsPage_PointerPressed;
            this.NavigationCacheMode = NavigationCacheMode.Required;

            CurrentInstance = this;
        }

        private void DetailsPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // handle Mouse back button (this can't be handled in KeyDown event handler)
            var isBackPressed = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed;
            if (isBackPressed) MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            illust = e.Parameter as IllustrationWrapper;
            var aid = long.Parse(illust.WrappedIllustration.ArtistUser.Id);

            if (ArtistId != aid) ArtistWorks = new PixivObservableCollection(a => a.GetUserIllustrations(ArtistId));

            ArtistId = aid;
            prevCollection = DownloadManager.CurrentCollection;
            DownloadManager.SwitchTo(ArtistWorks, illust.AssociatedAccount);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            DownloadManager.SwitchTo(prevCollection, illust.AssociatedAccount);
        }
    }
}
