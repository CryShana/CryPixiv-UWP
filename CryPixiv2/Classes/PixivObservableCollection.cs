﻿using CryPixiv2.Wrappers;
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
        private HashSet<int> addedIds = new HashSet<int>();
        private const int SpeedUpLimit = 100;
        private bool wasReset = false;
        #endregion

        #region Public Properties
        public TimeSpan Interval { get => interval; set { interval = value; AddTimer.Interval = value; } }
        public TimeSpan FastInterval => TimeSpan.FromMilliseconds(5);
        public ObservableCollection<IllustrationWrapper> Collection { get; }
        public ConcurrentQueue<IllustrationWrapper> EnqueuedItems { get; }
        public ConcurrentQueue<IllustrationWrapper> EnqueuedItemsForDirectInsertion { get; }
        public Dictionary<int, DateTime> LoadedElements = new Dictionary<int, DateTime>();
        public event EventHandler<IllustrationWrapper> ItemAdded; 
        #endregion

        public PixivObservableCollection(
            Func<PixivAccount, Task<IllustrationResponse>> getItems = null)
        {
            this.getItems = getItems;

            Collection = new ObservableCollection<IllustrationWrapper>();
            EnqueuedItems = new ConcurrentQueue<IllustrationWrapper>();
            EnqueuedItemsForDirectInsertion = new ConcurrentQueue<IllustrationWrapper>();

            AddTimer = new DispatcherTimer();
            AddTimer.Interval = Interval;
            AddTimer.Tick += AddTimer_Tick;
            AddTimer.Start();
        }

        private void AddTimer_Tick(object sender, object e)
        {
            if (EnqueuedItemsForDirectInsertion.IsEmpty == false)
            {
                if (EnqueuedItemsForDirectInsertion.TryDequeue(out IllustrationWrapper it) == false) return;
                Collection.Add(it);
                Collection.Move(Collection.IndexOf(it), 0);

                addedIds.Add(it.WrappedIllustration.Id);
                ItemAdded?.Invoke(this, it);
                return;
            }

            if (EnqueuedItems.IsEmpty) return;           
            if (EnqueuedItems.TryDequeue(out IllustrationWrapper item) == false) return;

            // speed up adder when threshold is exceeded
            if (EnqueuedItems.Count > SpeedUpLimit) AddTimer.Interval = FastInterval;          
            else AddTimer.Interval = Interval;
            
            // check for duplicates
            if (addedIds.Contains(item.WrappedIllustration.Id)) return;

            // add to collection and register illustration ID as added to avoid adding duplicates
            Collection.Add(item);
            addedIds.Add(item.WrappedIllustration.Id);
            LoadedElements.Add(item.WrappedIllustration.Id, DateTime.Now);

            ItemAdded?.Invoke(this, item);
        }

        public void Reset()
        {
            wasReset = true;

            EnqueuedItemsForDirectInsertion.Clear();
            EnqueuedItems.Clear();
            LoadedElements.Clear();
            Collection.Clear();
            addedIds.Clear();
        }
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
