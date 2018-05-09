using CryPixiv2.Classes;
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

        #region Private Fields
        private SlowObservableCollection<IllustrationWrapper> bookmarksPublic = new SlowObservableCollection<IllustrationWrapper>();
        private SlowObservableCollection<IllustrationWrapper> bookmarksPrivate = new SlowObservableCollection<IllustrationWrapper>();
        private SlowObservableCollection<IllustrationWrapper> recommended = new SlowObservableCollection<IllustrationWrapper>();
        private SlowObservableCollection<IllustrationWrapper> followingPublic = new SlowObservableCollection<IllustrationWrapper>();
        private SlowObservableCollection<IllustrationWrapper> followingPrivate = new SlowObservableCollection<IllustrationWrapper>();
        #endregion

        #region Public Properties
        public SlowObservableCollection<IllustrationWrapper> BookmarksPublic { get => bookmarksPublic; set { bookmarksPublic = value; Changed(); } }
        public SlowObservableCollection<IllustrationWrapper> BookmarksPrivate { get => bookmarksPrivate; set { bookmarksPrivate = value; Changed(); } }
        public SlowObservableCollection<IllustrationWrapper> Recommended { get => recommended; set { recommended = value; Changed(); } }
        public SlowObservableCollection<IllustrationWrapper> FollowingPublic { get => followingPublic; set { followingPublic = value; Changed(); } }
        public SlowObservableCollection<IllustrationWrapper> FollowingPrivate { get => followingPrivate; set { followingPrivate = value; Changed(); } } 
        #endregion
    }
}
