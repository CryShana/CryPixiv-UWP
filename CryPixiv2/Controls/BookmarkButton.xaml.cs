using CryPixiv2.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private void btnImage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Clicked?.Invoke(this, DataContext as IllustrationWrapper);
        }
    }
}
