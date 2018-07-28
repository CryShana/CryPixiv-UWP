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

        #region Helper Properties
        public int ImagesCount => (WrappedIllustration.MetaSinglePage.Count == 1 && WrappedIllustration.MetaPages.Count == 0) ? 1 : WrappedIllustration.MetaPages.Count;
        public bool HasMultipleImages => ImagesCount > 1;
        public event EventHandler<BitmapImage> ImageDownloaded;
        public bool ImageDownloadedSubscribed => ImageDownloaded != null;
        public string CreatedText => WrappedIllustration.Created.ToString("dd.MM.yyyy HH:mm");
        public string IllustrationLink => WrappedIllustration == null ? "" :
            $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={WrappedIllustration.Id.ToString()}";
        public string ArtistLink => WrappedIllustration == null ? "" : $"https://www.pixiv.net/member.php?id=" + WrappedIllustration.ArtistUser.Id;
        public string Description => WrappedIllustration.Caption.Replace("<br/>", "\n");
        public Dictionary<int, long> FileSizes { get; set; } = new Dictionary<int, long>();
        public Dictionary<int, string> Resolutions { get; set; } = new Dictionary<int, string>();
        #endregion

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

                GetImage(WrappedIllustration.FullImagePath, (i, ind) => FullImage = i, 0);
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
        #endregion

        #region Artist Image
        BitmapImage artistImage = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BitmapImage ArtistImage
        {
            get
            {
                if (artistImage != null) return artistImage;

                GetImage(WrappedIllustration.ArtistUser.ProfileImageUrls.First().Value, (i, ind) => ArtistImage = i);
                return artistImage;
            }
            set
            {
                artistImage = value;
                Changed();
            }
        }
        #endregion

        public bool IsBookmarked { get => WrappedIllustration.IsBookmarked; set { WrappedIllustration.IsBookmarked = value; Changed(); } }

        public IllustrationWrapper(Illustration illustration, PixivAccount account)
        {
            WrappedIllustration = illustration;
            AssociatedAccount = account;
        }

        #region Private Methods
        async Task GetImage(string path, Action<BitmapImage, int> callback, int index = -1)
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

                FileSizes.TryAdd(index, data.LongLength);
                Resolutions.TryAdd(index, $"{image.PixelWidth}x{image.PixelHeight}");

                callback(image, index);
                ImageDownloaded?.Invoke(this, image);
            }
        }
        public string GetOtherImagePath(int index)
        {
            var img = WrappedIllustration.MetaPages[index].ImageUrls;
            var key = "original";
            if (img.ContainsKey(key) == false) key = img.LastOrDefault().Key;
            return img[key];
        }
        async Task GetOtherImages()
        {
            for (int i = 1; i < WrappedIllustration.MetaPages.Count; i++)
            {
                var path = GetOtherImagePath(i);

                GetImage(path, (bi, ind) =>
                {
                    OtherImages[ind - 1] = bi;
                    Changed($"OtherImages[{ind - 1}]");
                    Changed("OtherImages");
                }, i);
            }
        }
        #endregion
    }
}
