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

        private int dspCount = 0, ldCount = 0, toldCount = 0;
        bool sortbkm = false;
        private string status = "Idle.";
        private AdvancedCollectionView viewSource = null;
        #endregion

        public PixivObservableCollection ItemSource { get => (PixivObservableCollection)GetValue(ItemSourceProperty); set => SetValue(ItemSourceProperty, value); }
        public int DisplayedCount => ItemSource.Collection.Count;
        public int LoadedCount => DisplayedCount + ToLoadCount;
        public int ToLoadCount => ItemSource.EnqueuedItems.Count;
        public string Status { get => status; private set { status = value; Changed(); } }
        public bool SortByBookmarks { get => sortbkm; set { sortbkm = value; Changed(); SortByBookmarkCount(value); } }

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
            };
            src.ItemsEnqueued += (a, b) =>
            {
                o.Changed("ToLoadCount");
                o.Changed("LoadedCount");
            };
        }

        private void SortByBookmarkCount(bool sort = true)
        {
            viewSource.SortDescriptions.Clear();
            if (sort) viewSource.SortDescriptions.Add(new SortDescription(SortDirection.Descending, new BookmarkComparer()));
            viewSource.Refresh();
        }
        private void GridView_Loaded(object sender, RoutedEventArgs e)
        {
            
            mylist.ItemClick += (a, b) =>
            {               
                var item = b.ClickedItem as IllustrationWrapper; 

                var package = new DataPackage();
                package.SetText(item.IllustrationLink);
                package.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(package);
                Clipboard.Flush();
            };
            mylist.IsItemClickEnabled = true;
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
            TimeSpan duration = TimeSpan.FromMilliseconds(900);

            var element = (Grid)sender;

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
