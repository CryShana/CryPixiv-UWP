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
    public class SlowObservableCollection<T>
    {
        #region Private Properties
        private DispatcherTimer AddTimer { get; }
        private TimeSpan interval = TimeSpan.FromMilliseconds(50); 
        #endregion

        #region Public Properties
        public TimeSpan Interval { get => interval; set { interval = value; AddTimer.Interval = value; } }
        public ObservableCollection<T> Collection { get; }
        public ConcurrentQueue<T> EnqueuedItems { get; }
        public event EventHandler<T> ItemAdded;
        public event EventHandler<T[]> ItemsEnqueued;
        #endregion

        public SlowObservableCollection()
        {
            Collection = new ObservableCollection<T>();
            EnqueuedItems = new ConcurrentQueue<T>();

            AddTimer = new DispatcherTimer();
            AddTimer.Interval = Interval;
            AddTimer.Tick += AddTimer_Tick;
            AddTimer.Start();
        }

        private void AddTimer_Tick(object sender, object e)
        {
            if (EnqueuedItems.IsEmpty) return;
            if (EnqueuedItems.TryDequeue(out T item) == false) return;

            Collection.Add(item);

            ItemAdded?.Invoke(this, item);
        }

        public void Add(params T[] items)
        {
            foreach (var i in items) EnqueuedItems.Enqueue(i);

            ItemsEnqueued?.Invoke(this, items);
        }
    }
}
