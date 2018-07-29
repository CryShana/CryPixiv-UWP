using CryPixiv2.Classes;
using CryPixiv2.Wrappers;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections;
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
using Windows.UI.Composition;
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

namespace CryPixiv2.Controls
{
    public sealed partial class IllustrationGrid : UserControl, INotifyPropertyChanged
    {
        #region Private Fields and PropertyChanged methods
        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register("ItemSource", typeof(PixivObservableCollection),
                typeof(IllustrationGrid), new PropertyMetadata(null, new PropertyChangedCallback(ItemSourceChanged)));

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed([CallerMemberName]string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        bool? sortbkm = false;
        private AdvancedCollectionView viewSource = null;
        #endregion

        public PixivObservableCollection ItemSource { get => (PixivObservableCollection)GetValue(ItemSourceProperty); set => SetValue(ItemSourceProperty, value); }
        public int DisplayedCount => ItemSource?.Collection?.Count ?? 0;
        public int LoadedCount => DisplayedCount + ToLoadCount;
        public int ToLoadCount => ItemSource?.EnqueuedItems?.Count ?? 0;
        public bool? SortByScore { get => sortbkm; set { sortbkm = value ?? false; Changed(); SortByBookmarkCount(value == true); } }
        public static event EventHandler<Tuple<IllustrationWrapper, bool>> IllustrationBookmarkChange;
        public static event EventHandler<IllustrationWrapper> ItemClicked;

        public IllustrationGrid()
        {
            this.InitializeComponent();
            viewSource = Resources["viewSource"] as AdvancedCollectionView;
        }

        public static void ItemSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var o = (IllustrationGrid)obj;
            var src = o.ItemSource;

            src.ItemAdded += (a, b) =>
            {
                o.Changed("ToLoadCount");
                o.Changed("DisplayedCount");
                o.Changed("LoadedCount");
            };
        }

        private void SortByBookmarkCount(bool sort = true)
        {
            viewSource.SortDescriptions.Clear();
            if (sort) viewSource.SortDescriptions.Add(new SortDescription(SortDirection.Descending, new BookmarkComparer()));
            viewSource.Refresh();
        }

        IllustrationWrapper _storedItem = null;
        const string thumbImageName = "thumbImage";
        private async void GridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_storedItem != null)
            {
                mylist.ScrollIntoView(_storedItem, ScrollIntoViewAlignment.Default);
                mylist.UpdateLayout();

                var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(Constants.ConnectedAnimationImage);
                if (animation != null) await mylist.TryStartConnectedAnimationAsync(animation, _storedItem, thumbImageName);

                _storedItem = null;
            }
        }

        #region Animations
        private void InitializeAnimations(Panel panel)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(mylist).Compositor;
            var elementImplicitAnimation = compositor.CreateImplicitAnimationCollection();
            elementImplicitAnimation["Offset"] = CreateOffsetAnimation(compositor);

            // panel animations
            ElementCompositionPreview.GetElementVisual(panel).ImplicitAnimations = elementImplicitAnimation;
        }

        private void DataTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            var element = (Grid)sender;

            // only display loaded item once          
            var item = (IllustrationWrapper)element.DataContext;
            if (item == null) return;
            if (ItemSource.LoadedElements.ContainsKey(item.WrappedIllustration.Id))
            {
                // Skip animation if Collection.LoadedElements contains this illustration for longer than X seconds
                var val = ItemSource.LoadedElements[item.WrappedIllustration.Id];
                if (DateTime.Now.Subtract(val).TotalSeconds > Constants.TimeTillAnimationSkipSeconds) return;
            }

            // prepare animation variables
            TimeSpan duration = TimeSpan.FromMilliseconds(900);

            var visual = ElementCompositionPreview.GetElementVisual(element);
            ElementCompositionPreview.SetIsTranslationEnabled(element, true); // if using Translation
            var compositor = visual.Compositor;
            var group = compositor.CreateAnimationGroup(); // if using multiple animations

            // prepare the opacity animation
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Duration = duration;
            opacityAnimation.InsertKeyFrame(0f, 0);
            opacityAnimation.InsertKeyFrame(1f, 1);
            opacityAnimation.Target = "Opacity";
            group.Add(opacityAnimation);

            // prepare the scale transform
            var scaleTransform = new ScaleTransform();
            element.RenderTransform = scaleTransform;

            var storyboard = new Storyboard();

            var scaleAnimationY = new DoubleAnimation();
            scaleAnimationY.From = 0.3;
            scaleAnimationY.To = 1;
            scaleAnimationY.Duration = duration;
            scaleAnimationY.EasingFunction = new CubicEase();
            Storyboard.SetTarget(scaleAnimationY, scaleTransform);
            Storyboard.SetTargetProperty(scaleAnimationY, "ScaleY");

            var scaleAnimationX = new DoubleAnimation();
            scaleAnimationX.From = 0.3;
            scaleAnimationX.To = 1;
            scaleAnimationX.Duration = duration;
            scaleAnimationX.EasingFunction = new CubicEase();
            Storyboard.SetTarget(scaleAnimationX, scaleTransform);
            Storyboard.SetTargetProperty(scaleAnimationX, "ScaleX");

            storyboard.Children.Add(scaleAnimationY);
            storyboard.Children.Add(scaleAnimationX);

            // start all the animations
            storyboard.Begin();
            visual.StartAnimationGroup(group);

            // LoadedElements.Add()
        }
        private void ItemsWrapGrid_Loaded(object sender, RoutedEventArgs e) => InitializeAnimations((Panel)sender);
        private static CompositionAnimationGroup CreateOffsetAnimation(Compositor compositor)
        {
            // Define Offset Animation for the Animation group
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertExpressionKeyFrame(1, "this.FinalValue");
            offsetAnimation.Duration = TimeSpan.FromSeconds(.8);

            // Define Animation Target for this animation to animate using definition. 
            offsetAnimation.Target = "Offset";

            // Add Animations to Animation group. 
            var animationGroup = compositor.CreateAnimationGroup();
            animationGroup.Add(offsetAnimation);

            return animationGroup;
        }
        #endregion

        private void btnBookmark_Click(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true; // so the post isn't clicked

            var work = ((Image)sender).DataContext as IllustrationWrapper;
            BookmarkWork(work, true);
        }

        private void BookmarkPrivate_Click(object sender, RoutedEventArgs e)
        {
            var work = (IllustrationWrapper)((MenuFlyoutItem)sender).DataContext;
            BookmarkWork(work, false);
        }

        public static async void BookmarkWork(IllustrationWrapper work, bool isPublic)
        {
            MainPage.Logger.Info($"{(work.IsBookmarked ? "Unbookmarking" : "Bookmarking")} work. " +
                $"(Id: {work.WrappedIllustration.Id}, IsPublic: {isPublic.ToString()})");

            if (work.IsBookmarked)
            {
                // unbookmark it
                work.IsBookmarked = false;

                try
                {
                    // remove bookmark
                    await work.AssociatedAccount.RemoveBookmark(work.WrappedIllustration.Id);
                    IllustrationBookmarkChange?.Invoke(null, new Tuple<IllustrationWrapper, bool>(work, isPublic));
                }
                catch (Exception ex)
                {
                    // fail - restore previous value
                    work.IsBookmarked = true;
                    MainPage.Logger.Error(ex, "Failed to remove bookmark!");
                }
            }
            else
            {
                // bookmark it
                work.IsBookmarked = true;

                try
                {
                    // add bookmark
                    await work.AssociatedAccount.AddBookmark(work.WrappedIllustration.Id, isPublic);
                    IllustrationBookmarkChange?.Invoke(null, new Tuple<IllustrationWrapper, bool>(work, isPublic));
                }
                catch (Exception ex)
                {
                    // fail - restore previous value
                    work.IsBookmarked = false;
                    MainPage.Logger.Error(ex, "Failed to add bookmark!");
                }
            }
        }

        private void mylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            var container = mylist.ContainerFromItem(e.ClickedItem) as GridViewItem;
            if (container != null)
            {
                // stash item
                _storedItem = container.Content as IllustrationWrapper;

                // prepare connected animation (name, stashed item, name of element that will be connected)
                var animation = mylist.PrepareConnectedAnimation(Constants.ConnectedAnimationThumbnail, _storedItem, thumbImageName);

                ItemClicked?.Invoke(this, _storedItem);
            }
            else
            {
                // handle this
            }
        }

        private async void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            var item = (IllustrationWrapper)((MenuFlyoutItem)sender).DataContext;
            if (await Windows.System.Launcher.LaunchUriAsync(new Uri(item.IllustrationLink)))
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }
    }

    public class BookmarkComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = (IllustrationWrapper)x;
            var b = (IllustrationWrapper)y;
            return a.WrappedIllustration.TotalBookmarks.CompareTo(b.WrappedIllustration.TotalBookmarks);
        }
    }
}
