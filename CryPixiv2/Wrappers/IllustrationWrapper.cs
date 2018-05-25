using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace CryPixiv2.Wrappers
{
    public class IllustrationWrapper : Notifier
    {
        public PixivAccount AssociatedAccount { get; set; }
        public Illustration WrappedIllustration { get; set; }

        public string IllustrationLink => WrappedIllustration == null ? "" : 
            $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={WrappedIllustration.Id.ToString()}";

        BitmapImage thumbnailImage = null;
        public bool ThumbnailImageLoading => thumbnailImage == null;
        public BitmapImage ThumbnailImage
        {
            get
            {
                if (thumbnailImage != null) return thumbnailImage;

                GetThumbnailImage();
                return null;
            }
            set
            {
                thumbnailImage = value;
                Changed();
                Changed("ThumbnailImageLoading");
            }
        }

        async Task GetThumbnailImage()
        {
            var data = await AssociatedAccount.GetData(WrappedIllustration.ThumbnailImagePath);

            using (var stream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                }
                var image = new BitmapImage();
                await image.SetSourceAsync(stream);
                ThumbnailImage = image;
            }
        }

        public IllustrationWrapper(Illustration illustration, PixivAccount account)
        {
            WrappedIllustration = illustration;
            AssociatedAccount = account;
        }
    }
}
