using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryPixiv2.Classes
{
    public static class DownloadManager
    {
        static Queue<CancellationTokenSource> tokens = new Queue<CancellationTokenSource>();

        public static void Stop()
        {
            // cancel all existing tokens
            while (tokens.Count > 0)
            {
                var t = tokens.Dequeue();
                t.Cancel();
            }
        }

        public static void SwitchTo(PixivObservableCollection collection, PixivAccount acc)
        {
            Stop();

            var src = new CancellationTokenSource();
            tokens.Enqueue(src);

            Task.Run(async () =>
            {
                try
                {
                    bool started = false;
                    while (true)
                    {
                        IllustrationResponse r = started == false ?
                            await collection.GetItems(acc) :
                            await collection.GetNextItems();

                        started = true;
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
                catch (COMException)
                {

                }
                catch
                {

                }                
            }, src.Token);
        }
    }
}
