using CryPixiv2.Classes;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace CryPixiv2
{
    public sealed partial class ArtistPage : Page, INotifyPropertyChanged
    {
        #region Stuff
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed([CallerMemberName]string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        PixivObservableCollection artistWorks, prevCollection;
        #endregion

        #region Properties
        public IllustrationWrapper Illustration { get; private set; }
        public long ArtistId { get; set; } = 0;
        private bool IsArtistFollowed
        {
            get => Illustration.WrappedIllustration.ArtistUser.IsFollowed;
            set
            {
                Illustration.WrappedIllustration.ArtistUser.IsFollowed = value;
                Changed();
            }
        } 
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

            Illustration = e.Parameter as IllustrationWrapper;
            var aid = long.Parse(Illustration.WrappedIllustration.ArtistUser.Id);

            if (ArtistId != aid) ArtistWorks = new PixivObservableCollection(a => a.GetUserIllustrations(ArtistId));

            ArtistId = aid;
            prevCollection = DownloadManager.CurrentCollection;
            DownloadManager.SwitchTo(ArtistWorks, Illustration.AssociatedAccount);

            // finish connected animation
            ConnectedAnimationService.GetForCurrentView().DefaultDuration = TimeSpan.FromSeconds(Constants.ArtistImageTransitionDuration);
            var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(Constants.ConnectedAnimationArtist);
            if (imageAnimation != null) imageAnimation.TryStart(artistImg);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            // prepare animation
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Constants.ConnectedAnimationArtistBack, artistImg);

            DownloadManager.SwitchTo(prevCollection, Illustration.AssociatedAccount);
        }

        private async void btnFollow_Click(object sender, RoutedEventArgs e)
        {
            bool oldval = IsArtistFollowed;
            followProgress.IsActive = true;
            try
            {
                long id = long.Parse(Illustration.WrappedIllustration.ArtistUser.Id);

                if (IsArtistFollowed == false) await Illustration.AssociatedAccount.FollowUser(id);
                else await Illustration.AssociatedAccount.UnfollowUser(id);

                IsArtistFollowed = !oldval;
            }
            catch
            {
                IsArtistFollowed = oldval;
            }
            finally
            {
                followProgress.IsActive = false;
            }
        }
    }
}
