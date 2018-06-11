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
using Windows.UI.Core;
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
    public sealed partial class DetailsPage : Page, INotifyPropertyChanged
    {
        #region Stuff
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed([CallerMemberName]string name = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        IllustrationWrapper illust = null; 
        #endregion
        public IllustrationWrapper Illustration { get => illust; set { illust = value; Changed(); } }

        public DetailsPage()
        {
            this.InitializeComponent();
            this.PointerPressed += DetailsPage_PointerPressed;   
        }

        private void DetailsPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var isBackPressed = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed;
            if (isBackPressed) MainPage.CurrentInstance.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.CurrentInstance.NavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            var item = e.Parameter as IllustrationWrapper;
            Illustration = item;

            var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(Constants.ConnectedAnimationThumbnail);
            if (imageAnimation != null) imageAnimation.TryStart(fullImage);          
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Constants.ConnectedAnimationImage, fullImage);
        }
    }
}
