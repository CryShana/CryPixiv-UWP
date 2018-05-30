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
        private PixivObservableCollection
            bookmarksPublic =       new PixivObservableCollection(a => a.GetBookmarks(true)),
            bookmarksPrivate =      new PixivObservableCollection(a => a.GetBookmarks(false)),
            recommended =           new PixivObservableCollection(a => a.GetRecommended()),
            followingPublic =       new PixivObservableCollection(a => a.GetNewestFollowingWorks(true)),
            followingPrivate =      new PixivObservableCollection(a => a.GetNewestFollowingWorks(false)),
            rankingDaily =          new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily)),
            rankingWeekly =         new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Weekly)),
            rankingMonthly =        new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Monthly)),
            rankingDailyMale =      new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Male)),
            rankingDailyFemale =    new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Female)),
            rankingDaily18 =        new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_R18)),
            rankingWeekly18 =       new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Weekly_R18)),
            rankingDailyMale18 =    new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Male_R18)),
            rankingDailyFemale18 =  new PixivObservableCollection(a => a.GetRankedIllustrations(PixivParameters.RankingMode.Daily_Female_R18));

        private ObservableCollection<SearchSession> searches = new ObservableCollection<SearchSession>();
        #endregion

        #region Public Properties
        public PixivObservableCollection BookmarksPublic        { get => bookmarksPublic;       set { bookmarksPublic = value; Changed(); } }
        public PixivObservableCollection BookmarksPrivate       { get => bookmarksPrivate;      set { bookmarksPrivate = value; Changed(); } }
        public PixivObservableCollection Recommended            { get => recommended;           set { recommended = value; Changed(); } }
        public PixivObservableCollection FollowingPublic        { get => followingPublic;       set { followingPublic = value; Changed(); } }
        public PixivObservableCollection FollowingPrivate       { get => followingPrivate;      set { followingPrivate = value; Changed(); } }
        public PixivObservableCollection RankingDaily           { get => rankingDaily;          set { rankingDaily = value; Changed(); } }
        public PixivObservableCollection RankingWeekly          { get => rankingWeekly;         set { rankingWeekly = value; Changed(); } }
        public PixivObservableCollection RankingMonthly         { get => rankingMonthly;        set { rankingMonthly = value; Changed(); } }
        public PixivObservableCollection RankingDailyMale       { get => rankingDailyMale;      set { rankingDailyMale = value; Changed(); } }
        public PixivObservableCollection RankingDailyFemale     { get => rankingDailyFemale;    set { rankingDailyFemale = value; Changed(); } }
        public PixivObservableCollection RankingDaily18         { get => rankingDaily18;        set { rankingDaily18 = value; Changed(); } }
        public PixivObservableCollection RankingWeekly18        { get => rankingWeekly18;       set { rankingWeekly18 = value; Changed(); } }
        public PixivObservableCollection RankingDailyMale18     { get => rankingDailyMale18;    set { rankingDailyMale18 = value; Changed(); } }
        public PixivObservableCollection RankingDailyFemale18   { get => rankingDailyFemale18;  set { rankingDailyFemale18 = value; Changed(); } }
        public ObservableCollection<SearchSession> Searches     { get => searches;              set { searches = value; Changed(); } }
        #endregion

        public MainViewModel()
        {

        }
    }
}
