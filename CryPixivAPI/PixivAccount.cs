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
            httpClient.DefaultRequestHeaders.Add("Referrer", BaseUrl);
        }
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
        private async Task<T> GetAsync<T>(string requestUri, Dictionary<string, string> values, string rootPropertyName = null)
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
                var msg = JObject.Parse(content).SelectToken("error").SelectToken("message").ToString();
                throw new Exception(msg);
            }
        }
        #endregion

        #region Public Methods
        public async Task Login(string username, string password) => await Login(username, password);
        public async Task Login(string refreshToken) => await Login(null, null, refreshToken);

        public async Task<IllustsResponse> SearchPosts(string query, string sortmode = PixivParameters.SearchSortMode.Descending,
            string target = PixivParameters.SearchTarget.PartialMatchForTags, string duration = PixivParameters.SearchDuration.None, 
            string overrideRequestUri = null)
        {
            var response = await GetAsync<IllustsResponse>(overrideRequestUri ?? "/v1/search/illust",
                overrideRequestUri != null ? null : new Dictionary<string, string>()
            {
                { "word", query },
                { "sort", sortmode },
                { "search_target", target },
                // { "bookmark_num", "0" },
                { "duration", duration }
            });
            response.AssociatedAccount = this;
            return response;
        }
        #endregion
    }

    public static class PixivExtensions
    {
        public static async Task<IllustsResponse> GetNextPage(this IllustsResponse response)
            => await response.AssociatedAccount.SearchPosts("", overrideRequestUri: response.NextUrl);
    }
}
