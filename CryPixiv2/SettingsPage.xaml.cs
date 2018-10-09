using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static CryPixiv2.ViewModels.MainViewModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CryPixiv2
{
    public sealed partial class SettingsPage : Page
    {
        List<string> doubleClickActions { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            this.PointerPressed += SettingsPage_PointerPressed;

            // add all enum value descriptions here in the SAME ORDER as defined in the enum definition (indexes should reflect enum values!)
            doubleClickActions = new List<string>()
            {
                "Toggle fullscreen",
                "Navigate back",
                "Copy image",
                "Go to next illustration"
            };
        }

        private void SettingsPage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // handle Mouse back button (this can't be handled in KeyDown event handler)
            var isBackPressed = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind == Windows.UI.Input.PointerUpdateKind.XButton1Pressed;
            if (isBackPressed) MainPage.CurrentInstance.HandleKey(Windows.System.VirtualKey.Back);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MainPage.CurrentInstance.NavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            _doubleClickAction.SelectedItem = doubleClickActions[(int)MainPage.CurrentInstance.ViewModel.PageAction_DetailsImageDoubleClick];
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            var action = (PageAction)Enum.Parse(typeof(PageAction), _doubleClickAction.SelectedIndex.ToString());
            MainPage.CurrentInstance.ViewModel.PageAction_DetailsImageDoubleClick = action;

            // save data
            MainPage.CurrentInstance.ViewModel.SaveData();
        }
    }
}
