using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryPixivAPI.Classes
{
    public class IllustrationResponse
    {
        [JsonProperty("illusts")]
        public List<Illustration> Illustrations { get; set; }

        [JsonProperty("next_url")]
        public string NextUrl { get; set; }

        [JsonProperty("search_span_limit")]
        public int SearchSpanLimit { get; set; }

        public Func<string, Task<IllustrationResponse>> GetNextPageAction { get; set; }
        public async Task<IllustrationResponse> NextPage()
        {
            if (string.IsNullOrEmpty(NextUrl)) throw new EndReachedException();
            return await GetNextPageAction(NextUrl);
        }
    }

    public class Illustration
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("image_urls")]
        public Dictionary<string, string> ImageUrls { get; set; }
        [JsonProperty("caption")]
        public string Caption { get; set; }
        [JsonProperty("restrict")]
        public int Restrict { get; set; }
        [JsonProperty("user")]
        public User ArtistUser { get; set; }
        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }
        [JsonProperty("tools")]
        public List<string> Tools { get; set; }
        [JsonProperty("create_date")]
        public DateTime Created { get; set; }
        [JsonProperty("page_count")]
        public int PageCount { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("sanity_level")]
        public int SanityLevel { get; set; }
        [JsonProperty("series")]
        public object Series { get; set; }
        [JsonProperty("meta_single_page")]
        public Dictionary<string, string> MetaSinglePage { get; set; }
        [JsonProperty("meta_pages")]
        public List<ImageUrlCollection> MetaPages { get; set; }
        [JsonProperty("total_view")]
        public int TotalViews { get; set; }
        [JsonProperty("total_bookmarks")]
        public int TotalBookmarks { get; set; }
        [JsonProperty("is_bookmarked")]
        public bool IsBookmarked { get; set; }
        [JsonProperty("visible")]
        public bool Visible { get; set; }
        [JsonProperty("is_muted")]
        public bool Muted { get; set; }

        public int Pages => MetaPages.Count == 0 ? 1 : MetaPages.Count;
        public string ThumbnailImagePath => ImageUrls["square_medium"];
        public string BigThumbnailImagePath => ImageUrls["medium"];
        public string FullImagePath
        {
            get
            {
                // this part of code is highly dependent on the key values (should improve it in the future)
                if (MetaPages.Count == 0 && MetaSinglePage.Count != 0)
                    return MetaSinglePage.ContainsKey("original_image_url") ?
                        MetaSinglePage["original_image_url"] : MetaSinglePage.First().Value;
                else if (MetaPages.Count == 0 && MetaSinglePage.Count == 0) return ImageUrls["large"];
                else return GetOriginalImagePath(0);              
            }
        }

        public string GetOriginalImagePath(int index) => MetaPages[index].ImageUrls["original"];
    }

    public class ImageUrlCollection
    {
        [JsonProperty("image_urls")]
        public Dictionary<string, string> ImageUrls { get; set; }
    }

    public class Tag
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}
