﻿using CryPixiv2.Classes;
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
using Windows.UI;
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
        #endregion

        #region Public Properties and Events
        public AdvancedCollectionView ViewSource { get; }
        public PixivObservableCollection ItemSource { get => (PixivObservableCollection)GetValue(ItemSourceProperty); set => SetValue(ItemSourceProperty, value); }
        public int DisplayedCount => ItemSource?.Collection?.Count ?? 0;
        public int LoadedCount => DisplayedCount + ToLoadCount;
        public int ToLoadCount => ItemSource?.EnqueuedItems?.Count ?? 0;
        public bool? SortByScore { get => sortbkm; set { sortbkm = value ?? false; Changed(); SortByBookmarkCount(value == true); } }
        public static event EventHandler<Tuple<IllustrationWrapper, bool>> IllustrationBookmarkChange;
        public static event EventHandler<IllustrationWrapper> ItemClicked; 
        #endregion

        public IllustrationGrid()
        {
            this.InitializeComponent();
            ViewSource = Resources["viewSource"] as AdvancedCollectionView;
        }

        public static void ItemSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var o = (IllustrationGrid)obj;
            var src = o.ItemSource;
            o.ItemSource.GridContainer = o;

            src.ItemAdded += (a, b) =>
            {
                o.Changed("ToLoadCount");
                o.Changed("DisplayedCount");
                o.Changed("LoadedCount");
            };
        }

        private void SortByBookmarkCount(bool sort = true)
        {
            ViewSource.SortDescriptions.Clear();
            if (sort) ViewSource.SortDescriptions.Add(new SortDescription(SortDirection.Descending, new BookmarkComparer()));
            ViewSource.Refresh();
        }

        IllustrationWrapper _storedItem = null;
        const string thumbImageName = "thumbImage";
        private async void GridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_storedItem != null)
            {
                mylist.ScrollIntoView(_storedItem, ScrollIntoViewAlignment.Default);
                // mylist.UpdateLayout();

                ConnectedAnimationService.GetForCurrentView().DefaultDuration = TimeSpan.FromSeconds(Constants.ImageTransitionDuration);
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

        #region Bookmarking
        private void BookmarkButton_Clicked(object sender, IllustrationWrapper work) => BookmarkWork(work, true);     

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
                    var exvar = ex;
                    MainPage.Logger.Error(exvar, "Failed to remove bookmark!");
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

                    var exvar = ex;
                    MainPage.Logger.Error(exvar, "Failed to add bookmark!");
                }
            }
        } 
        #endregion

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

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // I am using DataContextChanged instead of Loaded event, because Loaded event doesn't get called when ItemSource is reset.

            var element = (Grid)sender;
            var item = (IllustrationWrapper)element.DataContext;
            if (item == null) return;

            // ensure that item gets animated ONLY ONCE - right when it gets added to Collection
            if (ItemSource.LoadedElements.ContainsKey(item.WrappedIllustration.Id))
            {             
                var val = ItemSource.LoadedElements[item.WrappedIllustration.Id];

                // Skip animation if Collection.LoadedElements contains this illustration for longer than X seconds
                if (DateTime.Now.Subtract(val).TotalMilliseconds > Constants.TimeTillAnimationSkipMs) return;
                // Once animation is done, no longer repeat it - remove X seconds to always skip it in the future.
                else ItemSource.LoadedElements[item.WrappedIllustration.Id] = 
                        ItemSource.LoadedElements[item.WrappedIllustration.Id].Subtract(TimeSpan.FromMilliseconds(Constants.TimeTillAnimationSkipMs));
            }

            // prepare animation variables
            TimeSpan duration = TimeSpan.FromMilliseconds(Constants.ItemGridEntryAnimationDurationMs);

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
        }

        private void GridItem_Click(object sender, PointerRoutedEventArgs e)
        {
            var isLeftClick = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.LeftButtonPressed;
            if (isLeftClick) ItemClick(((Image)sender).DataContext as IllustrationWrapper);
        }
        
        public void ItemClick(IllustrationWrapper e)
        {
            var container = mylist.ContainerFromItem(e) as GridViewItem;
            if (container != null)
            {
                // stash item
                _storedItem = container.Content as IllustrationWrapper;

                try
                {
                    // prepare connected animation (name, stashed item, name of element that will be connected)
                    mylist.PrepareConnectedAnimation(Constants.ConnectedAnimationThumbnail, _storedItem, thumbImageName); 
                }
                catch { }

                ItemClicked?.Invoke(this, _storedItem);
            }
            else
            {
                // handle this
            }
        }


        public int AllowLevel
        {
            get => MainPage.CurrentInstance.ViewModel.AllowLevel;
            set
            {
                MainPage.CurrentInstance.ViewModel.AllowLevel = value;
                Changed();
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
