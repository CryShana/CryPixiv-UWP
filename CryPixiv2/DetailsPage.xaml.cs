using CryPixiv2.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
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

        #region Private Fields
        bool pageslidervis = false;
        #endregion

        #region Public Properties
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
        #endregion

        #region Private Properties
        private int MaxSelectedIndex => Illustration.ImagesCount - 1;
        private bool PageSliderVisible { get => pageslidervis; set { pageslidervis = value; Changed(); } }
        #endregion

        public DetailsPage()
        {
            this.InitializeComponent();
            this.PointerPressed += DetailsPage_PointerPressed;
            this.PreviewKeyDown += DetailsPage_PreviewKeyDown;
        }

        private void DetailsPage_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var isBackPressed = 
                e.Key == Windows.System.VirtualKey.Escape || 
                e.Key == Windows.System.VirtualKey.Back || 
                e.Key == Windows.System.VirtualKey.GoBack;
            if (isBackPressed) MainPage.CurrentInstance.GoBack();
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
            if (Illustration.ImageDownloadedSubscribed == false) Illustration.ImageDownloaded += (a, b) => progress.IsActive = IsCurrentPageLoading;
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

        private async Task<byte[]> GetImageData(int selectedIndex)
        {
            // get image url and download it again 
            // (because it's too damn complicated converting existing BitmapImage to byte array while also handling situations where image isn't loaded yet)
            var uri = (selectedIndex == 0) ? Illustration.WrappedIllustration.FullImagePath : Illustration.GetOtherImagePath(selectedIndex);
            return await Illustration.AssociatedAccount.GetData(uri);
        }
        private async void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await GetImageData(_flipview.SelectedIndex);

                InMemoryRandomAccessStream rstream = new InMemoryRandomAccessStream();
                await rstream.WriteAsync(data.AsBuffer());
                rstream.Seek(0);

                // copy to clipboard
                var package = new DataPackage();
                package.SetBitmap(RandomAccessStreamReference.CreateFromStream(rstream));
                package.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(package);
                Clipboard.Flush();
            }
            catch
            {
                // show error
            }
        }

        private string GetFileName(int index) => $"{Illustration.WrappedIllustration.Id}_p{index}.png";
        private async void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileSavePicker picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.Downloads;
                picker.SuggestedFileName = GetFileName(_flipview.SelectedIndex);
                picker.FileTypeChoices.Add("PNG file", new[] { ".png" });
                var d = await picker.PickSaveFileAsync();

                var data = await GetImageData(_flipview.SelectedIndex);
                await FileIO.WriteBytesAsync(d, data);
            }
            catch
            {
                // show error
            }
        }

        private async void SaveAllImages_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.Downloads;
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                var d = await picker.PickSingleFolderAsync();

                for (int i = 0; i < Illustration.ImagesCount; i++)
                {
                    var data = await GetImageData(i);
                    await FileIO.WriteBytesAsync(await d.CreateFileAsync(GetFileName(i), CreationCollisionOption.GenerateUniqueName), data);
                }
            }
            catch
            {
                // show error
            }
        }

        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (await Windows.System.Launcher.LaunchUriAsync(new Uri(Illustration.IllustrationLink)))
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }

        private void PageCounter_Click(object sender, PointerRoutedEventArgs e)
        {
            PageSliderVisible = !PageSliderVisible;
        }
    }
}
