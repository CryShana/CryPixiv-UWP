using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryPixiv2.Classes
{
    public static class DownloadManager
    {
        static Queue<CancellationTokenSource> tokens = new Queue<CancellationTokenSource>();

        public static void SwitchTo(PixivObservableCollection collection, PixivAccount acc)
        {
            // cancel all existing ones
            while (tokens.Count > 0)
            {
                var t = tokens.Dequeue();
                t.Cancel();
            }

            var src = new CancellationTokenSource();
            tokens.Enqueue(src);

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        IllustrationResponse r = collection.Collection.Count == 0 ?
                            await collection.GetItems(acc) :
                            await collection.GetNextItems();

                        foreach (var l in r.Illustrations)
                        {
                            src.Token.ThrowIfCancellationRequested();
                            var wr = new IllustrationWrapper(l, acc);
                            collection.Add(wr);
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
                catch (EndReachedException)
                {

                }
                catch
                {

                }                
            }, src.Token);
        }
    }
}
