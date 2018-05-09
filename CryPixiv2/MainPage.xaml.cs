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

        public MainPage()
        {
            this.InitializeComponent();

            CurrentInstance = this;
            ViewModel = (MainViewModel)Application.Current.Resources["mainViewModel"];
           
            DoStuff();
        }


        public async void DoStuff()
        {
            ViewModel.Account = new PixivAccount("fa2226814b46768e9f0ea3aafac61eb6");
            await ViewModel.Account.Login("IuEsI8_15UjDFtSfaOcqJkPCK3oe12IzQDMwP4mz_qA");
            
            var ill = await ViewModel.Account.GetNewestFollowingWorks();
            addStuff(ill);

            async void addStuff(IllustrationResponse r)
            {
                foreach (var l in r.Illustrations) ViewModel.Illusts.Add(new IllustrationWrapper(l, ViewModel.Account));

                try
                {
                    var np = await r.NextPage();
                    addStuff(np);
                }
                catch { }
            }        
        }
    }
}
