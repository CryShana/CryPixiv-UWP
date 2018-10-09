using CryPixivAPI;
using CryPixivAPI.Classes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        #region Sanity Properties
        // if SanityLevel <= 2 --> SAFE
        // if SanityLevel <= 4 --> SAFE
        // if SanityLevel <= 6 --> QUESTIONABLE
        // if SanityLevel > 6 and R-18 tag --> NSFW
        public bool IsNsfw => WrappedIllustration.SanityLevel >= 6 && WrappedIllustration.Tags.Count(x => x.Name.ToLower() == "r-18") > 0;
        public bool IsQuestionable => WrappedIllustration.SanityLevel >= 5 && !IsNsfw;
        public bool IsSafe => WrappedIllustration.SanityLevel < 5;
        public bool IsBlurred
        {
            get
            {
                switch (MainPage.CurrentInstance.ViewModel.AllowLevel)
                {
                    case 0: return IsNsfw || IsQuestionable;
                    case 1: return IsNsfw;
                    case 2: return false;
                    default: return false;
                }
            }
        }
        #endregion

        public int ImagesCount => (WrappedIllustration.MetaSinglePage.Count == 1 && WrappedIllustration.MetaPages.Count == 0) ? 1 : WrappedIllustration.MetaPages.Count;
        public bool HasMultipleImages => ImagesCount > 1;
        public event EventHandler<BitmapImage> ImageDownloaded;
        public bool ImageDownloadedSubscribed => ImageDownloaded != null;
        public string CreatedText => WrappedIllustration.Created.ToString("dd.MM.yyyy HH:mm");
        public string IllustrationLink => WrappedIllustration == null ? "" :
            $"https://www.pixiv.net/member_illust.php?mode=medium&illust_id={WrappedIllustration.Id.ToString()}";
        public string ArtistLink => WrappedIllustration == null ? "" : $"https://www.pixiv.net/member.php?id=" + WrappedIllustration.ArtistUser.Id;
        public string Description
        {
            get
            {
                var txt = WrappedIllustration.Caption.Replace("<br/>", "\n").Replace("<br />", "\n");
                txt = System.Web.HttpUtility.HtmlDecode(txt);
                if (string.IsNullOrEmpty(txt)) return "-";

                var rep = Regex.Replace(txt, @"<[^>]*>", "");
                return rep;
            }
        }
        public ConcurrentDictionary<int, long> FileSizes { get; set; } = new ConcurrentDictionary<int, long>();
        public ConcurrentDictionary<int, string> Resolutions { get; set; } = new ConcurrentDictionary<int, string>();
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

                GetImage(WrappedIllustration.ThumbnailImagePath, (i, ind, b) => ThumbnailImage = i);
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

                GetImage(WrappedIllustration.FullImagePath, (i, ind, b) =>
                {
                    FullImage = i;
                    FullImageRawData = b;
                }, 0);
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
                    OtherImagesRawData = new byte[ImagesCount - 1][];
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
        public byte[] FullImageRawData { get; private set; }
        public byte[][] OtherImagesRawData { get; private set; }
        #endregion

        #region Artist Image
        BitmapImage artistImage = null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BitmapImage ArtistImage
        {
            get
            {
                if (artistImage != null) return artistImage;

                GetImage(WrappedIllustration.ArtistUser.ProfileImageUrls.First().Value, (i, ind, b) => ArtistImage = i);
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

            MainPage.CurrentInstance.ViewModel.AllowLevelChanged += (a, b) => Changed("IsBlurred");
        }

        #region Private Methods
        async Task GetImage(string path, Action<BitmapImage, int, byte[]> callback, int index = -1)
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

                callback(image, index, data);
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

                GetImage(path, (bi, ind, b) =>
                {
                    var index = ind - 1;
                    OtherImages[index] = bi;
                    OtherImagesRawData[index] = b;

                    Changed($"OtherImages[{ind - 1}]");
                    Changed("OtherImages");
                }, i);
            }
        }
        #endregion
    }
}
