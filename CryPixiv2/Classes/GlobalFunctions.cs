using CryPixiv2.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
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

        public static async Task CopyToClipboardBitmap(byte[] data, string filename = null)
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

        public static string GetImageUrl(IllustrationWrapper illust, int pageIndex)
            => (pageIndex == 0) ? illust.WrappedIllustration.FullImagePath : illust.GetOtherImagePath(pageIndex);

        public static string GetIllustrationFileName(IllustrationWrapper illust, int pageIndex)
            => Path.GetFileName(GetImageUrl(illust, pageIndex));

        public static byte[] Serialize(object data)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, data);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return (T)new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
