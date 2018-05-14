using CryPixivAPI.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CryPixivAPI
{
    public class PixivAccount
    {
        #region Private Constants
        private const string BaseUrl = "https://app-api.pixiv.net";
        private const string BaseAuthUrl = "https://oauth.secure.pixiv.net";
        private const string UserAgent = "PixivAndroidApp/5.0.61 (Android 6.0)";
        private const string ClientId = "MOBrBDS8blbauoSck0ZfDbtuzpyT";
        private const string ClientSecret = "lsACyCD94FhDUtGTXi3QzcFE2uU1hqtDaKeqrdwj";
        #endregion

        protected HttpClient httpClient;
        protected HttpClient authClient;

        #region Public Properties
        public AuthResponse AuthInfo { get; private set; }
        #endregion

        public PixivAccount(string deviceToken = null)
        {
            InitializeHttpClient();

            AuthInfo = new AuthResponse() { DeviceToken = deviceToken };
        }

        #region Private Methods
        private void InitializeHttpClient()
        {
            authClient = new HttpClient()
            {
                BaseAddress = new Uri(BaseAuthUrl)
            };

            httpClient = new HttpClient()
            {
                BaseAddress = new Uri(BaseUrl)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            httpClient.DefaultRequestHeaders.Add("Referer", BaseUrl);
        }
        public async Task<byte[]> GetData(string uri) => await httpClient.GetByteArrayAsync(uri);      
        private async Task Login(string username, string password, string refreshToken = null)
        {
            var values = (refreshToken == null) ? new Dictionary<string, string>()
            {
                { "username", username },
                { "password", password },
                { "grant_type", "password" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "device_token", AuthInfo?.DeviceToken ?? "" }
            } : new Dictionary<string, string>()
            {
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "device_token", AuthInfo?.DeviceToken ?? "" }
            };

            DateTime issued = DateTime.Now;
            var response = await authClient.PostAsync("/auth/token", new FormUrlEncodedContent(values));
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                content = JObject.Parse(content).SelectToken("response").ToString();
                AuthInfo = JsonConvert.DeserializeObject<AuthResponse>(content);
                AuthInfo.TimeIssued = issued;

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthInfo.AccessToken);
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(content);
                throw new LoginException(error.Errors.System.Message);
            }
        }
        private async Task<T> GetAsync<T>(string requestUri, Dictionary<string, string> values = null, string rootPropertyName = null)
        {
            if (AuthInfo == null || AuthInfo?.AccessToken == null) throw new LoginException("User must be logged in!");

            // build request uri
            int i = 0;
            string uri = requestUri;
            if (values != null) foreach (var v in values)
                {
                    if (v.Value == null) continue;
                    uri += ((i == 0 && !uri.Contains("?")) ? "?" : "&") + v.Key + "=" + HttpUtility.HtmlEncode(v.Value);
                    i++;
                }

            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (rootPropertyName != null) content = JObject.Parse(content).SelectToken(rootPropertyName).ToString();
                var obj = JsonConvert.DeserializeObject<T>(content);
                return obj;
            }
            else
            {
                var err = JObject.Parse(content).SelectToken("error");
                var msg = err.SelectToken("message").ToString();
                if (string.IsNullOrEmpty(msg)) msg = err.SelectToken("user_message").ToString();

                throw new Exception(msg);
            }
        }
        private async Task<T> PostAsync<T>(string requestUri, Dictionary<string, string> values = null, string rootPropertyName = null)
        {
            if (AuthInfo == null || AuthInfo?.AccessToken == null) throw new LoginException("User must be logged in!");

            var response = await httpClient.PostAsync(requestUri, new FormUrlEncodedContent(values ?? new Dictionary<string, string>()));
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (rootPropertyName != null) content = JObject.Parse(content).SelectToken(rootPropertyName).ToString();
                var obj = JsonConvert.DeserializeObject<T>(content);
                return obj;
            }
            else
            {
                var err = JObject.Parse(content).SelectToken("error");
                var msg = err.SelectToken("message").ToString();
                if (string.IsNullOrEmpty(msg)) msg = err.SelectToken("user_message").ToString();

                throw new Exception(msg);
            }
        }
        #endregion

        #region Public Methods
        public async Task Login(string username, string password) => await Login(username, password, null);
        public async Task Login(string refreshToken) => await Login(null, null, refreshToken);

        public async Task<IllustrationResponse> SearchPosts(string query, string sortmode = PixivParameters.SearchSortMode.Descending,
            string target = PixivParameters.SearchTarget.PartialMatchForTags, string duration = PixivParameters.SearchDuration.None,
            string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v1/search/illust",
                overrideRequestUri != null ? null : new Dictionary<string, string>()
                {
                    { "word", query },
                    { "sort", sortmode },
                    { "search_target", target },
                    // { "bookmark_num", "0" },
                    { "duration", duration }
                });
            response.GetNextPageAction = (x) => SearchPosts("", overrideRequestUri: x);
            return response;
        }

        public async Task<IllustrationResponse> GetBookmarks(bool isPublic = true, string tag = null, string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v1/user/bookmarks/illust",
               overrideRequestUri != null ? null : new Dictionary<string, string>()
               {
                   { "user_id", AuthInfo?.User?.Id },
                   { "restrict", isPublic ? "public" : "private" },
                   { "tag", tag }
               });
            response.GetNextPageAction = (x) => GetBookmarks(isPublic, overrideRequestUri: x);
            return response;
        }
        public async Task<IllustrationResponse> GetRecommended(string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v1/illust/recommended");
            response.GetNextPageAction = (x) => GetRecommended(overrideRequestUri: x);
            return response;
        }
        public async Task<IllustrationResponse> GetUserIllustrations(long user_id, string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v1/user/illusts",
               overrideRequestUri != null ? null : new Dictionary<string, string>()
               {
                   { "user_id", user_id.ToString() }
                   //{ "type", "" }
               });
            response.GetNextPageAction = (x) => GetUserIllustrations(user_id, overrideRequestUri: x);
            return response;
        }
        public async Task<IllustrationResponse> GetRelatedIllustrations(long illust_id, string overrideRequestUri = null)
        {
            throw new NotImplementedException("Specified endpoint does not exist yet!");

            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v2/illust/related ",
               overrideRequestUri != null ? null : new Dictionary<string, string>()
               {
                   { "illust_id", illust_id.ToString() }
               });
            response.GetNextPageAction = (x) => GetRelatedIllustrations(illust_id, overrideRequestUri: x);
            return response;
        }
        public async Task<IllustrationResponse> GetRankedIllustrations(string mode = PixivParameters.RankingMode.Daily, string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v1/illust/ranking",
                new Dictionary<string, string>()
                {
                    { "mode", mode }
                });

            response.GetNextPageAction = (x) => GetRankedIllustrations(overrideRequestUri: x);
            return response;
        }
        public async Task<IllustrationResponse> GetNewestFollowingWorks(bool isPublic = true, string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustrationResponse>(overrideRequestUri ?? "/v2/illust/follow",
                new Dictionary<string, string>()
                {
                    { "restrict", isPublic ? "public" : "private" }
                });

            response.GetNextPageAction = (x) => GetNewestFollowingWorks(isPublic, overrideRequestUri: x);
            return response;
        }
        public async Task AddBookmark(long illust_id, bool isPublic = true)
        {
            var response = await PostAsync<object>("/v2/illust/bookmark/add",
                new Dictionary<string, string>()
                {
                    { "illust_id", illust_id.ToString() },
                    { "restrict",  isPublic ? "public" : "private" }
                });

            // can also accept 'tags[]' or 'tags' parameter (list of strings) ?
        }
        public async Task RemoveBookmark(long illust_id)
        {
            var response = await PostAsync<object>("/v1/illust/bookmark/delete",
                new Dictionary<string, string>()
                {
                    { "illust_id", illust_id.ToString() }
                });
        }

        public async Task FollowUser(long user_id, bool isPublic = true)
        {
            var response = await PostAsync<object>("/v1/user/follow/add",
                new Dictionary<string, string>()
                {
                    { "user_id", user_id.ToString() },
                    { "restrict",  isPublic ? "public" : "private" }
                });
        }
        public async Task UnfollowUser(long user_id)
        {
            var response = await PostAsync<object>("/v1/user/follow/delete",
                new Dictionary<string, string>()
                {
                    { "user_id", user_id.ToString() }
                });
        }
        #endregion
    }
}
