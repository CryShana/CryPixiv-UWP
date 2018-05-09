using CryPixiv2.Classes;
using CryPixiv2.Wrappers;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.UI.Xaml.Navigation;

namespace CryPixiv2.Controls
{
    public sealed partial class IllustrationGrid : UserControl, INotifyPropertyChanged
    {
        #region Private Fields and PropertyChanged methods
        public static readonly DependencyProperty ItemSourceProperty =
            DependencyProperty.Register("ItemSource", typeof(SlowObservableCollection<IllustrationWrapper>), 
                typeof(IllustrationGrid), new PropertyMetadata(null, new PropertyChangedCallback(ItemSourceChanged)));

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed([CallerMemberName]string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private int dspCount = 0, ldCount = 0, toldCount = 0;
        private string status = "Idle.";
        private AdvancedCollectionView viewSource = null;
        #endregion

        public SlowObservableCollection<IllustrationWrapper> ItemSource { get => (SlowObservableCollection<IllustrationWrapper>)GetValue(ItemSourceProperty); set => SetValue(ItemSourceProperty, value); }
        public int DisplayedCount { get => dspCount; private set { dspCount = value; Changed(); } }
        public int LoadedCount { get => ldCount; private set { ldCount = value; Changed(); } }
        public int ToLoadCount { get => toldCount; private set { toldCount = value; Changed(); } }
        public string Status { get => status; private set { status = value; Changed(); } }

        public IllustrationGrid()
        {
            this.InitializeComponent();
            viewSource = Resources["viewSource"] as AdvancedCollectionView;
            
            // viewSource.SortDescriptions.Add(new SortDescription("WrappedIllustration.TotalBookmarks", SortDirection.Descending));
        }

        public static void ItemSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var o = (IllustrationGrid)obj;
            var src = o.ItemSource;
           
            src.ItemAdded += (a, b) =>
            {
                o.ToLoadCount = o.ItemSource.EnqueuedItems.Count;
                o.DisplayedCount = o.ItemSource.Collection.Count;
            };
            src.ItemsEnqueued += (a, b) =>
            {
                o.ToLoadCount = o.ItemSource.EnqueuedItems.Count;
                o.LoadedCount = o.DisplayedCount + o.ToLoadCount;
            };
        }

        #region Animations
        private void GridView_Loaded(object sender, RoutedEventArgs e)
        {
            mylist.ItemClick += (a, b) =>
            {
                var item = b.ClickedItem as IllustrationWrapper;
                var dialog = new MessageDialog(item.WrappedIllustration.Id.ToString(), "Selected Item");

                var package = new DataPackage();
                package.SetText("https://www.pixiv.net/member_illust.php?mode=medium&illust_id=" + item.WrappedIllustration.Id.ToString());
                package.RequestedOperation = DataPackageOperation.Copy;
                Clipboard.SetContent(package);
                Clipboard.Flush();

                dialog.ShowAsync();
            };
            mylist.IsItemClickEnabled = true;
        }

        private void InitializeAnimations(Panel panel)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(mylist).Compositor;
            var elementImplicitAnimation = compositor.CreateImplicitAnimationCollection();
            elementImplicitAnimation["Offset"] = CreateOffsetAnimation(compositor);

            // panel animations
            ElementCompositionPreview.GetElementVisual(panel).ImplicitAnimations = elementImplicitAnimation;

            // item animations
            mylist.LayoutUpdated += (a, b) =>
            {               
                // try to optimize this
                for (int i = 0; i < ItemSource.Collection.Count; i++)
                {
                    var itemContainer = (GridViewItem)mylist.ContainerFromItem(ItemSource.Collection[i]);
                    if (itemContainer == null) break;
                    
                    var visual = ElementCompositionPreview.GetElementVisual(itemContainer);
                    if (visual.ImplicitAnimations != null) continue;
                    visual.ImplicitAnimations = elementImplicitAnimation;
                }
            };
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
}
