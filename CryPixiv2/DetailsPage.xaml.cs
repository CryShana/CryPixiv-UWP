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
        public bool IsCurrentPageLoading
        {
            get
            {
                if (Illustration == null) return true;
                else if (Illustration.HasMultipleImages && _flipview.SelectedIndex > 0) return Illustration.OtherImages[_flipview.SelectedIndex - 1] == null;
                else return Illustration.FullImageLoading;
            }
        }
        public int CurrentPage => _flipview.SelectedIndex + 1;
        public string PageCounter => $"{CurrentPage} / {Illustration.ImagesCount}";

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

            // only subscribe if not subscribed yet
            if (Illustration.ImageDownloadedSubscribed == false) Illustration.ImageDownloaded += (a,b) => progress.IsActive = IsCurrentPageLoading;
            // check progress and hide it if already loaded
            progress.IsActive = IsCurrentPageLoading;

            var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(Constants.ConnectedAnimationThumbnail);
            if (imageAnimation != null) imageAnimation.TryStart(fullImage);

            // remove other images from flipview
            if (_flipview.Items.Count > 1)
                for (int i = _flipview.Items.Count - 1; i >= 1; i--) _flipview.Items.RemoveAt(i);

            // add other images
            if (item.HasMultipleImages)
            {
                int i = 0;
                foreach (var img in item.WrappedIllustration.MetaPages.Skip(1))
                {
                    Image imgElement = new Image();
                    Binding b = new Binding();
                    b.Source = item;
                    b.Path = new PropertyPath($"OtherImages[{i}]");
                    b.Mode = BindingMode.OneWay;
                    BindingOperations.SetBinding(imgElement, Image.SourceProperty, b);
                    _flipview.Items.Add(imgElement);
                    i++;
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            try
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Constants.ConnectedAnimationImage, (Image)_flipview.Items[_flipview.SelectedIndex]);
            }
            catch
            {
                // might throw exception if element not in view
            }
        }

        private void _flipview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            progress.IsActive = IsCurrentPageLoading;
            Changed("CurrentPage");
            Changed("PageCounter");
        }
        
    }
}
