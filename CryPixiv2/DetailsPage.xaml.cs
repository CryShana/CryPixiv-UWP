﻿using CryPixiv2.Classes;
using CryPixiv2.Controls;
using CryPixiv2.Wrappers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
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
        bool pageslidervis = false;
        bool pageLoading = true;
        #endregion

        #region Public Properties
        public PixivObservableCollection AssociatedCollection { get; private set; }
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
        private bool IsArtistFollowed
        {
            get => Illustration.WrappedIllustration.ArtistUser.IsFollowed;
            set
            {
                Illustration.WrappedIllustration.ArtistUser.IsFollowed = value;
                Changed();
            }
        }
        private string CurrentImageResolution
        {
            get
            {
                if (Illustration.Resolutions.TryGetValue(_flipview.SelectedIndex, out string resl)) return resl;
                else return "-";
            }
        }
        private string CurrentImageSize
        {
            get
            {
                if (Illustration.FileSizes.TryGetValue(_flipview.SelectedIndex, out long size))
                {
                    var kbsize = size / 1024.0;
                    if (kbsize > 1024.0) return $"{Math.Round(kbsize / 1024.0, 2)}MB";
                    else return $"{Math.Round(kbsize, 2)}kB";
                }
                else return "-";
            }
        }
        private double DescriptionMinWidth => tagsGrid.ActualWidth + artistGrid.ActualWidth + detailGrid.ActualWidth + descriptionGrid.ActualWidth;
        private double DetailMinWidth => tagsGrid.ActualWidth + artistGrid.ActualWidth + detailGrid.ActualWidth;
        #endregion

        public DetailsPage()
        {
            this.InitializeComponent();
            this.PointerPressed += DetailsPage_PointerPressed;
            this.SizeChanged += (a, b) =>
            {
                Changed("DescriptionMinWidth");
                Changed("DetailMinWidth");
            };
        }

        private void DetailsPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // handle Mouse back button (this can't be handled in KeyDown event handler)
            var isBackPressed = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed;
            if (isBackPressed) MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            pageLoading = true;
            base.OnNavigatedTo(e);
            MainPage.CurrentInstance.NavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            var item = e.Parameter as Tuple<PixivObservableCollection, IllustrationWrapper>;
            AssociatedCollection = item.Item1;
            Illustration = item.Item2;

            // only subscribe if not subscribed yet
            if (Illustration.ImageDownloadedSubscribed == false) Illustration.ImageDownloaded += (a, b) => _flipview_SelectionChanged(null, null);
            // check progress and hide it if already loaded
            progress.IsActive = IsCurrentPageLoading;

            // finish connected animation
            ConnectedAnimationService.GetForCurrentView().DefaultDuration = TimeSpan.FromSeconds(Constants.ImageTransitionDuration);
            var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation(Constants.ConnectedAnimationThumbnail);
            if (imageAnimation != null) imageAnimation.TryStart(fullImage);
            
            // remove other images from flipview
            if (_flipview.Items.Count > 1) for (int i = _flipview.Items.Count - 1; i >= 1; i--) _flipview.Items.RemoveAt(i);

            // add other images
            if (Illustration.HasMultipleImages)
            {
                int i = 0;
                foreach (var img in Illustration.WrappedIllustration.MetaPages.Skip(1))
                {
                    Image imgElement = new Image();
                    Binding b = new Binding();
                    b.Source = Illustration;
                    b.Path = new PropertyPath($"OtherImages[{i}]");
                    b.Mode = BindingMode.OneWay;
                    BindingOperations.SetBinding(imgElement, Image.SourceProperty, b);
                    _flipview.Items.Add(imgElement);
                    i++;
                }
            }

            await Task.Delay(400).ConfigureAwait(false);
            pageLoading = false;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (ignoreNav)
            {
                ignoreNav = false;
                return;
            }
            else
            {
                try
                {
                    if (!isArtist)
                    {
                        // When going to MainPage
                        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Constants.ConnectedAnimationImage, (Image)_flipview.Items[_flipview.SelectedIndex]);
                    }
                    else
                    {
                        // When going to ArtistPage
                        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(Constants.ConnectedAnimationArtist, artistImg);
                    }
                }
                catch
                {
                    // might throw exception if element not in view
                }
            }

            base.OnNavigatingFrom(e);
        }

        private void _flipview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            progress.IsActive = IsCurrentPageLoading;
            Changed("CurrentPage");
            Changed("PageCounter");
            Changed("CurrentImageResolution");
            Changed("CurrentImageSize");
        }

        #region ContextMenu Action and Bookmark Button
        private async Task<byte[]> GetImageData(int selectedIndex)
        {
            if (selectedIndex == 0 && Illustration.FullImageRawData == null) return null;
            else if (selectedIndex > 0 && Illustration.OtherImagesRawData.Length < selectedIndex) return null;

            return selectedIndex == 0 ? Illustration.FullImageRawData : Illustration.OtherImagesRawData[selectedIndex - 1];
        }
        private async void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowNotification("Copying image...", true);

                var data = await GetImageData(_flipview.SelectedIndex);

                await GlobalFunctions.CopyToClipboardBitmap(data,
                    GlobalFunctions.GetIllustrationFileName(Illustration, _flipview.SelectedIndex));

                ShowNotification("Image copied.");
            }
            catch (Exception ex)
            {
                // show error
                ShowNotification("Failed to copy image!");
                MainPage.Logger.Error(ex, "Failed to copy image!");
            }
        }

        private async void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileSavePicker picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.Downloads;
                picker.SuggestedFileName = GlobalFunctions.GetIllustrationFileName(Illustration, _flipview.SelectedIndex);
                picker.FileTypeChoices.Add("Image file", new[] { ".png", ".jpg" });
                var d = await picker.PickSaveFileAsync();
                if (d == null) return;

                ShowNotification("Saving image...", true);

                var data = await GetImageData(_flipview.SelectedIndex);
                await FileIO.WriteBytesAsync(d, data);

                ShowNotification("Image saved.");
            }
            catch (Exception ex)
            {
                // show error
                ShowNotification("Failed to save image!");
                MainPage.Logger.Error(ex, "Failed to save image!");
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
                if (d == null) return;

                ShowNotification("Saving images...", true);

                for (int i = 0; i < Illustration.ImagesCount; i++)
                {
                    var data = await GetImageData(i);
                    await FileIO.WriteBytesAsync(await d.CreateFileAsync(GlobalFunctions.GetIllustrationFileName(Illustration, i),
                        CreationCollisionOption.GenerateUniqueName), data);
                }

                ShowNotification("Images saved.");
            }
            catch (Exception ex)
            {
                // show error
                ShowNotification("Failed to save all images!");
                MainPage.Logger.Error(ex, "Failed to save all images!");
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
                ShowNotification("Failed to open in browser!");
            }
        }

        private void BookmarkButton_Clicked(object sender, IllustrationWrapper e) => IllustrationGrid.BookmarkWork(Illustration, true);

        private void bookmarkPrivatelyItem_Click(object sender, RoutedEventArgs e) => IllustrationGrid.BookmarkWork(Illustration, false);
        #endregion

        #region PageCounter Animations
        private void PageCounter_Click(object sender, PointerRoutedEventArgs e)
            => PageSliderVisible = !PageSliderVisible;

        private void pageCounterGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
            => VisualStateManager.GoToState(this, "state_gridMouseOver", false);

        private void pageCounterGrid_PointerExited(object sender, PointerRoutedEventArgs e)
            => VisualStateManager.GoToState(this, "state_gridMouseExit", false);
        #endregion

        public void ShowNotification(string text, bool keepAlive = false)
        {
            MainPage.Logger.Info("Notification shown: " + text);
            notification.Show(text, keepAlive ? 0 : Constants.InAppNotificationDuration);
        }

        #region Artist Grid
        private void ArtistGrid_Entered(object sender, PointerRoutedEventArgs e)
            => VisualStateManager.GoToState(this, "state_agridMouseOver", false);

        private void ArtistGrid_Exited(object sender, PointerRoutedEventArgs e)
            => VisualStateManager.GoToState(this, "state_agridMouseExit", false);

        bool isArtist = false;
        private void ArtistGrid_Click(object sender, PointerRoutedEventArgs e)
        {
            var rclick = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.RightButtonPressed;
            if (rclick) return;     

            // open artist in another page
            var frame = Window.Current.Content as Frame;
            bool wasArtistPagePrevious = frame.BackStack.Count > 1;

            if (wasArtistPagePrevious) MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
            else
            {
                isArtist = true;
                NavigateTo(typeof(ArtistPage), Illustration);
            }
        }

        public void NavigateTo(Type pageType, object referencedObject) => Frame.Navigate(pageType, referencedObject, new DrillInNavigationTransitionInfo());
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
        private async void ArtistOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (await Windows.System.Launcher.LaunchUriAsync(new Uri(Illustration.ArtistLink)))
            {
                // URI launched
            }
            else
            {
                // URI launch failed
                ShowNotification("Failed to open in browser!");
            }
        }
        #endregion

        #region Tags
        private void tagsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tagsCombobox.SelectedItem == null) return;
            var tag = ((dynamic)tagsCombobox.SelectedItem).Name as string;

            GlobalFunctions.CopyToClipboardText(tag);

            ShowNotification("Tag copied to clipboard.");

            tagsCombobox.SelectedItem = null;
        }
        private async void TagTranslationTextblock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
        {
            var textblock = (TextBlock)sender;
            var tag = ((dynamic)textblock.DataContext).Name as string;

            var txt = "";
            // take existing translation
            if (MainPage.CurrentInstance.ViewModel.TranslatedWords.ContainsKey(tag)) txt = MainPage.CurrentInstance.ViewModel.TranslatedWords[tag];
            else
            {
                // translate it
                txt = await Task.Run(() => Translator.Translate(tag));
                if (string.IsNullOrEmpty(txt) == false) MainPage.CurrentInstance.ViewModel.TranslatedWords.TryAdd(tag, txt);
            }
          
            textblock.Text = txt;
        }
        #endregion

        #region Navigation
        bool ignoreNav = false;
        public void NextIllustration()
        {
            var g = AssociatedCollection.GridContainer;
            var i = g.ViewSource.IndexOf(Illustration);

            var c = g.ViewSource.Count;
            for (int ix = 1; ix < c; ix++)
            {
                // check boundaries
                if (i + ix >= g.ViewSource.Count)
                {
                    ShowNotification("No next illustration!");
                    return;
                }

                // check item
                var e = (IllustrationWrapper)g.ViewSource.ElementAt(i + ix);
                if (e.IsBlurred) continue;

                ignoreNav = true;
                MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
                g.ItemClick(e);
                break;
            }
        }

        public void PreviousIllustration()
        {
            var g = AssociatedCollection.GridContainer;
            var i = g.ViewSource.IndexOf(Illustration);
            var c = g.ViewSource.Count;
            for (int ix = 1; ix < c; ix++)
            {
                // check boundaries
                if (i - ix < 0)
                {
                    ShowNotification("No previous illustration!");
                    return;
                }

                // check item
                var e = (IllustrationWrapper)g.ViewSource.ElementAt(i - ix);
                if (e.IsBlurred) continue;

                ignoreNav = true;
                MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
                g.ItemClick(e);
                break;
            }
        }
        #endregion

        private void Page_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            switch (MainPage.CurrentInstance.ViewModel.PageAction_DetailsImageDoubleClick)
            {
                case ViewModels.MainViewModel.PageAction.ToggleFullscreen:
                    var view = ApplicationView.GetForCurrentView();
                    if (view.IsFullScreenMode) view.ExitFullScreenMode();
                    else view.TryEnterFullScreenMode(); 
                    break;
                case ViewModels.MainViewModel.PageAction.NavigateBack:
                    if (pageLoading) return;

                    MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
                    break;
                case ViewModels.MainViewModel.PageAction.CopyImage:
                    CopyImage_Click(this, null);
                    break;
                case ViewModels.MainViewModel.PageAction.GoToNextIllustration:
                    if (pageLoading) return;

                    NextIllustration();
                    break;
            }
        }
    }
}
