using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public int ImagesCount => (WrappedIllustration.MetaSinglePage.Count == 1 && WrappedIllustration.MetaPages.Count == 0) ? 1 : WrappedIllustration.MetaPages.Count;
        public bool HasMultipleImages => ImagesCount > 1;

        public string IllustrationLink => WrappedIllustration == null ? "" :
            $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={WrappedIllustration.Id.ToString()}";

        #region Thumbnail Image Fetching
        BitmapImage thumbnailImage = null;
        public bool ThumbnailImageLoading => thumbnailImage == null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BitmapImage ThumbnailImage
        {
            get
            {
                if (thumbnailImage != null) return thumbnailImage;

                GetImage(WrappedIllustration.ThumbnailImagePath, (i, ind) => ThumbnailImage = i);
                return null;
            }
            set
            {
                thumbnailImage = value;
                Changed();
                Changed("ThumbnailImageLoading");
            }
        }
        #endregion

        #region Full Image Fetching
        BitmapImage fullImage = null;
        public bool FullImageLoading => fullImage == null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BitmapImage FullImage
        {
            get
            {
                if (fullImage != null) return fullImage;

                GetImage(WrappedIllustration.FullImagePath, (i, ind) => FullImage = i);
                return thumbnailImage;
            }
            set
            {
                fullImage = value;
                Changed();
                Changed("FullImageLoading");
            }
        }
        #endregion

        #region Other Image Fetching
        public bool[] OtherImageLoading = null;
        BitmapImage[] otherImages = null;
        public BitmapImage[] OtherImages
        {
            get
            {
                if (otherImages == null)
                {
                    otherImages = new BitmapImage[ImagesCount - 1];
                    GetOtherImages();
                }
                return otherImages;
            }
            set
            {
                otherImages = value;
                Changed();
            }
        }

        // create array of bitmapimages
        // start downloading ALL OF THEM when method is called (*when work is opened)
        // call Changed event on every download finish
        // bind to array property

        // LATER: Download thumbnails first (these have square_medium and medium sizes too)
        #endregion

        public bool IsBookmarked { get => WrappedIllustration.IsBookmarked; set { WrappedIllustration.IsBookmarked = value; Changed(); } }

        public IllustrationWrapper(Illustration illustration, PixivAccount account)
        {
            WrappedIllustration = illustration;
            AssociatedAccount = account;
        }

        #region Private Methods
        async Task GetImage(string path, Action<BitmapImage, int> callback, int index = 0)
        {
            var data = await AssociatedAccount.GetData(path);
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                }
                var image = new BitmapImage();
                await image.SetSourceAsync(stream);

                callback(image, index);
            }
        }
        async Task GetOtherImages()
        {
            for (int i = 1; i < WrappedIllustration.MetaPages.Count; i++)
            {
                var img = WrappedIllustration.MetaPages[i].ImageUrls;
                var key = "original";
                if (img.ContainsKey(key) == false) key = img.LastOrDefault().Key;
                var path = img[key];

                GetImage(path, (bi, ind) => { OtherImages[ind - 1] = bi; Changed($"OtherImages[{ind - 1}]"); Changed("OtherImages"); }, i);
            }
        }
        #endregion
    }
}
