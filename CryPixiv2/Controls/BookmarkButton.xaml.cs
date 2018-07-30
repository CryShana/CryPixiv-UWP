using CryPixiv2.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CryPixiv2.Controls
{
    public sealed partial class BookmarkButton : UserControl
    {       
        public event EventHandler<IllustrationWrapper> Clicked;
        public BookmarkButton()
        {
            this.InitializeComponent();
        }

        private async void btnImage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var illust = DataContext as IllustrationWrapper;
            if (illust == null) return;

            if (illust.IsBookmarked == false)
            {
                // bookmark animation
                firstStoryboard.Begin();
                await Task.Delay(300);
                secondStoryboard.Begin();
            }
            else
            {
                // unbookmark animation
                thirdStoryboard.Begin();
            }

            Clicked?.Invoke(this, illust);
        }
    }
}
