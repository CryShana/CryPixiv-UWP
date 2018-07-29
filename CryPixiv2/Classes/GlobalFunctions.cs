using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace CryPixiv2.Classes
{
    public static class GlobalFunctions
    {
        public static void CopyToClipboardText(string text)
        {
            var p = new DataPackage();
            p.SetText(text);
            p.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(p);
            Clipboard.Flush();
        }

        public static async Task CopyToClipboardBitmap(byte[] data)
        {
            InMemoryRandomAccessStream rstream = new InMemoryRandomAccessStream();
            await rstream.WriteAsync(data.AsBuffer());
            rstream.Seek(0);

            var package = new DataPackage();
            package.SetBitmap(RandomAccessStreamReference.CreateFromStream(rstream));
            package.RequestedOperation = DataPackageOperation.Copy;
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
    }
}
