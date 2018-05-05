using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CryPixiv2.ViewModels
{
    public class MainViewModel : Notifier
    {
        public PixivAccount Account { get; set; }

        private ObservableCollection<IllustrationWrapper> illusts = new ObservableCollection<IllustrationWrapper>();
        public ObservableCollection<IllustrationWrapper> Illusts { get => illusts; set { illusts = value; Changed(); } }

    }
}
