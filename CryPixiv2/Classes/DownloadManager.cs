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
        public static bool IsPaused { get; set; } = false;
        public static PixivObservableCollection CurrentCollection { get; private set; }

        public static void Stop()
        {
            // cancel all existing tokens
            while (tokens.Count > 0)
            {
                var t = tokens.Dequeue();
                t.Cancel();
            }
        }
        

        /// <summary>
        /// This controls what Collection will be getting downloaded
        /// </summary>
        /// <param name="collection">Collection to download</param>
        /// <param name="acc">Associated account</param>
        public static void SwitchTo(PixivObservableCollection collection, PixivAccount acc)
        {
            Stop();

            var src = new CancellationTokenSource();
            tokens.Enqueue(src);

            CurrentCollection = collection;

            Task.Run(async () =>
            {
                try
                {
                    bool started = false;
                    while (true)
                    {
                        // check if paused
                        if (IsPaused)
                        {
                            await Task.Delay(500);
                            continue;
                        }

                        // Infinite loop that will initially call the "getItems" callback and later on "getNextItems" on the collection
                        IllustrationResponse r = started == false ?
                            await collection.GetItems(acc) :
                            await collection.GetNextItems();

                        started = true;
                        foreach (var l in r.Illustrations)
                        {
                            // go through all downloaded illustractions and wrap them up - if duplicates, take existing to save memory
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
                    MainPage.Logger.Info("End Reached!");
                }
                catch (COMException cex)
                {
                    var cexvar = cex;
                    MainPage.Logger.Error(cexvar, "COM Exception!");
                }
                catch (LoginException lex)
                {
                    // user is not logged in
                    MainPage.Logger.Error(lex, "User not logged in!");

                    // retry login
                    var vm = MainPage.CurrentInstance.ViewModel;
                    vm.Login(vm.Account.AuthInfo.RefreshToken);
                }
                catch (OffsetLimitException oex)
                {
                    var oexvar = oex;
                    MainPage.Logger.Error(oexvar, "Offset limit reached!");
                }
                catch (Exception ex)
                {
                    var exvar = ex;
                    MainPage.Logger.Error(exvar, "Unknown exception! " + exvar.Message + (exvar.InnerException != null ? exvar.InnerException.Message : ""));
                }                
            }, src.Token);
        }
    }
}
