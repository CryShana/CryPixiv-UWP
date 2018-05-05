using CryPixivAPI;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CryPixiv2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Hello();
        }

        public async void Hello()
        {
            var pixiv = new PixivAccount("3b1c31a804e9f2b624837f683ef06a55");

            await pixiv.Login("IuEsI8_15UjDFtSfaOcqJkPCK3oe12IzQDMwP4mz_qA");
            var response = await pixiv.GetRecommended();
            var response2 = await pixiv.GetRecommendedUsers();

        }
    }
}
