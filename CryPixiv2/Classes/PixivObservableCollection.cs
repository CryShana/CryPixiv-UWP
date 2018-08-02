using CryPixiv2.Controls;
using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace CryPixiv2.Classes
{
    public class PixivObservableCollection
    {
        #region Private Properties
        private DispatcherTimer AddTimer { get; }
        private TimeSpan interval = TimeSpan.FromMilliseconds(50);
        private Func<PixivAccount, Task<IllustrationResponse>> getItems;
        private IllustrationResponse lastResponse;
        private PixivAccount account;
        private const int SpeedUpLimit = 100;
        private bool wasReset = false;
        #endregion

        #region Public Properties
        public IllustrationGrid GridContainer { get; set; }
        public TimeSpan Interval { get => interval; set { interval = value; AddTimer.Interval = value; } }
        public TimeSpan FastInterval => TimeSpan.FromMilliseconds(5);

        // This is the main collection - you should ItemSource to this
        public ObservableCollection<IllustrationWrapper> Collection { get; } = new ObservableCollection<IllustrationWrapper>();
        public ConcurrentQueue<IllustrationWrapper> EnqueuedItems { get; } = new ConcurrentQueue<IllustrationWrapper>();
        public ConcurrentQueue<IllustrationWrapper> EnqueuedItemsForDirectInsertion { get; } = new ConcurrentQueue<IllustrationWrapper>();

        // This dictionary keeps track of already added elements sorted in a binary tree for fast duplicate checking
        // DateTimes are saved to keep animation from repeating after X seconds from being added to collection
        public Dictionary<int, DateTime> LoadedElements = new Dictionary<int, DateTime>(); 

        public event EventHandler<IllustrationWrapper> ItemAdded;
        public bool IsStarted { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;
        #endregion

        public PixivObservableCollection(
            Func<PixivAccount, Task<IllustrationResponse>> getItems = null)
        {
            this.getItems = getItems;

            AddTimer = new DispatcherTimer();
            AddTimer.Interval = Interval;
            AddTimer.Tick += AddTimer_Tick;
            AddTimer.Start();
        }

        private void AddTimer_Tick(object sender, object e)
        {
            // check if paused
            if (IsPaused) return;
            
            if (EnqueuedItemsForDirectInsertion.IsEmpty == false)
            {
                if (EnqueuedItemsForDirectInsertion.TryDequeue(out IllustrationWrapper it) == false) return;
                if (LoadedElements.ContainsKey(it.WrappedIllustration.Id)) return;

                Collection.Add(it);
                Collection.Move(Collection.IndexOf(it), 0);

                LoadedElements.Add(it.WrappedIllustration.Id, DateTime.Now);
                ItemAdded?.Invoke(this, it);
                return;
            }

            if (EnqueuedItems.IsEmpty) return;           
            if (EnqueuedItems.TryDequeue(out IllustrationWrapper item) == false) return;

            // speed up adder when threshold is exceeded
            if (EnqueuedItems.Count > SpeedUpLimit) AddTimer.Interval = FastInterval;          
            else AddTimer.Interval = Interval;
            
            // check for duplicates
            if (LoadedElements.ContainsKey(item.WrappedIllustration.Id)) return;

            // add to collection and register illustration ID as added to avoid adding duplicates
            Collection.Add(item);

            LoadedElements.Add(item.WrappedIllustration.Id, DateTime.Now);
            ItemAdded?.Invoke(this, item);
        }

        public void Reset()
        {
            wasReset = true;
            IsStarted = false;
            lastResponse = null;

            EnqueuedItemsForDirectInsertion.Clear();
            EnqueuedItems.Clear();
            LoadedElements.Clear();
            Collection.Clear();
        }
        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public void Insert(params IllustrationWrapper[] items)
        {
            // if Reset was called, ignore any further queues until initial getItems is called
            if (wasReset) return;

            // enqueue items
            foreach (var i in items) EnqueuedItemsForDirectInsertion.Enqueue(i);
        }

        public void Add(params IllustrationWrapper[] items)
        {
            // if Reset was called, ignore any further queues until initial getItems is called
            if (wasReset) return;

            // enqueue items
            foreach (var i in items) EnqueuedItems.Enqueue(i);
        }

        public async Task<IllustrationResponse> GetItems(PixivAccount account)
        {
            this.account = account;

            // get initial items that will also provide a NextUrl for further items
            var r = await getItems?.Invoke(account);
            lastResponse = r;

            if (lastResponse != null) IsStarted = true;
            return r;
        }
        public async Task<IllustrationResponse> GetNextItems()
        {
            // if Reset was called and account is set, call the initial getItems
            if (wasReset && this.account != null)
            {
                // disable Reset mode to allow queueing items
                wasReset = false;
                return await GetItems(this.account);
            }

            // get items from NextUrl that was received in the last response
            var r = await lastResponse.NextPage();
            lastResponse = r;
            return r;
        }
    }
}
