using CryPixiv2.ViewModels;
using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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


namespace CryPixiv2
{
    public sealed partial class MainPage : Page
    {
        public static MainPage CurrentInstance;
        public MainViewModel ViewModel;

        static CompositionAnimationGroup CreateOffsetAnimation(Compositor compositor)
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

        public MainPage()
        {
            this.InitializeComponent();

            CurrentInstance = this;
            ViewModel = (MainViewModel)Application.Current.Resources["mainViewModel"];

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

            
            DoStuff();
        }

        private void ItemsWrapGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(mylist).Compositor;
            var elementImplicitAnimation = compositor.CreateImplicitAnimationCollection();
            elementImplicitAnimation["Offset"] = CreateOffsetAnimation(compositor);

            mylist.ChoosingItemContainer += async (a, b) =>
            {
                await Task.Delay(400);
                var itemContainer = (GridViewItem)mylist.ContainerFromItem(b.Item);
                var visual = ElementCompositionPreview.GetElementVisual(itemContainer);
                visual.ImplicitAnimations = elementImplicitAnimation;
            };          
        }

        public async void DoStuff()
        {
            ViewModel.Account = new PixivAccount("fa2226814b46768e9f0ea3aafac61eb6");
            await ViewModel.Account.Login("IuEsI8_15UjDFtSfaOcqJkPCK3oe12IzQDMwP4mz_qA");
            
            var ill = await ViewModel.Account.GetRecommended();
            addStuff(ill);

            async void addStuff(IllustrationResponse r)
            {
                foreach (var l in r.Illustrations) ViewModel.Illusts.Add(new IllustrationWrapper(l, ViewModel.Account));
                
                var np = await r.NextPage();
                addStuff(np);
            }        
        }
    }
}
