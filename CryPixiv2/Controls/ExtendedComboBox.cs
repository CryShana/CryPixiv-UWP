using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CryPixiv2.Controls
{
    public class ExtendedComboBox : ComboBox
    {
        public ScrollViewer ScrollViewerComponent { get; private set; }

        public ExtendedComboBox()
        {
            DefaultStyleKey = typeof(ComboBox);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ScrollViewerComponent = GetTemplateChild("ScrollViewer") as ScrollViewer;
            if (ScrollViewerComponent != null)
            {
                ScrollViewerComponent.Loaded += OnScrollViewerLoaded;
            }
        }

        private void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            ScrollViewerComponent.Loaded -= OnScrollViewerLoaded;
            ScrollViewerComponent.ChangeView(null, ScrollViewerComponent.ScrollableHeight, null);
        }
    }
}
