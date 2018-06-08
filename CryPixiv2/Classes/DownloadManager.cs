using CryPixiv2.Wrappers;
using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        static ConcurrentDictionary<int, IllustrationWrapper> addedIllustrations = new ConcurrentDictionary<int, IllustrationWrapper>();

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

                            IllustrationWrapper wr = null;
                            if (addedIllustrations.ContainsKey(l.Id))
                            {
                                // take existing Illustration
                                wr = addedIllustrations[l.Id];
                            }
                            else
                            {
                                wr = new IllustrationWrapper(l, acc);
                                addedIllustrations.TryAdd(l.Id, wr);
                            }

                            collection.Add(wr);
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
                catch (EndReachedException)
                {
                    Debug.WriteLine("End Reached!");
                }
                catch (COMException)
                {
                    Debug.WriteLine("COM Exception!");
                }
                catch (LoginException)
                {
                    // user is not logged in
                    Debug.WriteLine("User not logged in!");

                    // retry login
                    var vm = MainPage.CurrentInstance.ViewModel;
                    vm.Login(vm.Account.AuthInfo.RefreshToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Unknown exception! " + ex.Message);
                }                
            }, src.Token);
        }
    }
}
