using CryPixivAPI.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
        }

        #region Public Methods
        public async Task Login(string username, string password)
        {
            var response = await authClient.PostAsync("/auth/token",
                new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "username", username },
                { "password", password },
                { "grant_type", "password" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "device_token", AuthInfo?.DeviceToken ?? "" }
            }));

            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                content = JObject.Parse(content).SelectToken("response").ToString();
                AuthInfo = JsonConvert.DeserializeObject<AuthResponse>(content);
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(content);
                throw new LoginException(error.Errors.System.Message);
            }
        }
        public async Task Login(string refreshToken)
        {
            var response = await authClient.PostAsync("/auth/token",
                new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "device_token", AuthInfo?.DeviceToken ?? "" }
            }));

            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                content = JObject.Parse(content).SelectToken("response").ToString();
                AuthInfo = JsonConvert.DeserializeObject<AuthResponse>(content);
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(content);
                throw new LoginException(error.Errors.System.Message);
            }
        } 
        #endregion
    }
}
