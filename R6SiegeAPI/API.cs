using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static R6SiegeAPI.Enums;
using static R6SiegeAPI.Models;

namespace R6SiegeAPI
{
    /// <summary>
    /// Holds your authentication information. Used to retrieve various objects.
    /// </summary>
    public class API
    {
        private static API instance;
        /// <summary>
        /// The current connections session id (will change upon attaining new key)
        /// </summary>
        string SessionId;
        /// <summary>
        /// Your token
        /// </summary>
        string Token;
        /// <summary>
        /// Your App Id
        /// </summary>
        string AppId;
        /// <summary>
        /// Your current auth key (will change every time you connect)
        /// </summary>
        string AuthKey;
        /// <summary>
        /// Locale
        /// </summary>
        public Locales LocaleKey { get; private set; }

        /// <summary>
        /// Contains the SpaceId for each platform
        /// </summary>
        public static Dictionary<string, string> SpaceIDs = new Dictionary<string, string>()
            {
                { "uplay", "5172a557-50b5-4665-b7db-e3f2e8c5041d" },
                { "psn", "05bfb3f7-6c21-4c42-be1f-97a33fb5cf66" },
                { "xbl", "98a601e5-ca91-4440-b1c5-753f601a2c90" }
            };

        public static string BaseUrl { get; set; } = "https://game-rainbow6.ubi.com/";
        public string OperatorsEndpoint { get; set; } = BaseUrl + "assets/data/operators.682af7ce0969c4e.json";
        public string WeaponsEndpoint { get; set; } = BaseUrl + "assets/data/weapons.8a9b3d9e7dadee0.json";
        public string SeasonsEndpoint { get; set; } = BaseUrl + "assets/data/seasons.791d0e62f4766f9.json";
        public string LocaleEndpoint { get; set; } = BaseUrl + "assets/locales/locale.en-us.f2aae574be115b9.json";


        private static string GetBasicToken(string email, string password)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(email + ':' + password));
        }

        /// <summary>
        /// The base class for API
        /// </summary>
        /// <param name="email">Your Ubisoft email</param>
        /// <param name="password">Your Ubisoft email</param>
        /// <param name="appId">Your Ubisoft appid, not required</param>
        /// <param name="locale">Locale, not required</param>
        /// <param name="tryLoadLast">if true library will try to load latest jsons automatically, not required</param>
        public static API InitAPI(string email, string password, string appId = "39baebad-39e5-4552-8c25-2c9b919064e2", Locales locale = Locales.en_us, bool tryLoadLast = true)
        {
            return InitAPI(GetBasicToken(email, password), appId, locale);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token">Your Ubisoft auth token</param>
        /// <param name="appId">Your Ubisoft appid, not required</param>
        /// <param name="locale">Locale, not required</param>
        /// <param name="tryLoadLast">if true library will try to load latest jsons automatically, not required</param>
        public static API InitAPI(string token, string appId = "39baebad-39e5-4552-8c25-2c9b919064e2", Locales locale = Locales.en_us, bool tryLoadLast = true)
        {
            if (!(instance is null))
                return instance;

            instance = new API
            {
                Token = token,
                AppId = appId,
                LocaleKey = locale
            };

            if (tryLoadLast)
            {
                try
                {
                   instance.LoadEndpoints(locale);
                }
                catch
                {
                    //
                }
            }

            return instance;
        }

        private void LoadEndpoints(Locales locale)
        {
            using (WebClient wc = new WebClient())
            {
                var html = wc.DownloadString(BaseUrl);

                var mainSearch = "<script src=\"assets/scripts/main.";
                var vendorSearch = "<script src=\"assets/scripts/vendor.";
                var endSearch = ".js\"></script>";

                string FindBetween(string str, string start, string end)
                {
                    var startPos = str.IndexOf(start) + start.Length;
                    var endPos = str.IndexOf(end, startPos);
                    return str.Substring(startPos, endPos - startPos);
                }

                var mainUrl = BaseUrl + "assets/scripts/main." + FindBetween(html, mainSearch, endSearch) + ".js";
                var vendorUrl = BaseUrl + "assets/scripts/vendor." + FindBetween(html, vendorSearch, endSearch) + ".js";

                var mainJs = wc.DownloadString(mainUrl);
                var vendorJs = wc.DownloadString(vendorUrl);

                var json = ".json";

                OperatorsEndpoint = BaseUrl + "assets/data/operators." + FindBetween(mainJs, "assets/data/operators.", json) + json;
                WeaponsEndpoint = BaseUrl + "assets/data/weapons." + FindBetween(mainJs, "assets/data/weapons.", json) + json;
                SeasonsEndpoint = BaseUrl + "assets/data/seasons." + FindBetween(mainJs, "assets/data/seasons.", json) + json;

                LocaleEndpoint = BaseUrl + $"assets/locales/locale.{locale.ToStringValue()}." + FindBetween(vendorJs, $"assets/locales/locale.{locale.ToStringValue()}.", json) + json;
            }
        }

        public static API GetAPI()
        {
            return instance;
        }

        public static void Dispose()
        {
            instance = null;
        }

        private async Task Connect()
        {
            var webRequest = WebRequest.Create("https://connect.ubi.com/ubiservices/v2/profiles/sessions");
            var HttpApiRequest = (HttpWebRequest)webRequest;
            HttpApiRequest.Method = "POST";
            HttpApiRequest.PreAuthenticate = true;
            HttpApiRequest.Headers.Add("Authorization", "Basic " + Token);
            HttpApiRequest.Headers.Add("Ubi-AppId", AppId);
            HttpApiRequest.Headers.Add("Content-Type", "application/json");
            using (var APIResponse = await HttpApiRequest.GetResponseAsync())
            {
                using (var responseStream = APIResponse.GetResponseStream())
                {
                    var StreamReader = new StreamReader(responseStream, Encoding.Default);
                    var data = (JObject)JsonConvert.DeserializeObject(StreamReader.ReadToEnd());

                    if (data.ContainsKey("ticket"))
                    {
                        AuthKey = data["ticket"].Value<string>();
                        SessionId = data["sessionId"].Value<string>();
                    }
                    else
                        throw new Exception("Couldn't connect to ubi servers.");
                }
            }
        }

        public async Task<string> GetAsync(string uri, WebHeaderCollection webHeader = null)
        {
            if (AuthKey is null)
            {
                await Connect();
            }

            if (webHeader is null)
            {
                webHeader = new WebHeaderCollection
                {
                    { "Authorization", "Ubi_v1 t=" + AuthKey },
                    { "Ubi-AppId", AppId },
                    { "Ubi-SessionId", SessionId },
                    { "Connection", "keep-alive" }
                };
            }

            var webRequest = WebRequest.Create(uri);
            var HttpApiRequest = (HttpWebRequest)webRequest;
            HttpApiRequest.Method = "GET";
            HttpApiRequest.PreAuthenticate = true;
            HttpApiRequest.Headers = webHeader;

            try
            {
                using (var APIResponse = await HttpApiRequest.GetResponseAsync())
                {
                    using (var responseStream = APIResponse.GetResponseStream())
                    {
                        var StreamReader = new StreamReader(responseStream, Encoding.Default);
                        return StreamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                HttpStatusCode? status = (ex.Response as HttpWebResponse)?.StatusCode;

                if (status.HasValue && status.Value == HttpStatusCode.Unauthorized)
                {
                    await Connect();
                    return await GetAsync(uri);
                }
                else
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        ///Get a player matching the term on that platform
        /// </summary>
        /// <param name="player">The name of the player you're searching for</param>
        /// <param name="platform">The name of the platform you're searching on</param>
        /// <param name="userSearchType">The type of search to perform</param>
        /// <returns>Player found</returns>
        public async Task<Player> GetPlayer(string player, Platform platform, UserSearchType userSearchType = UserSearchType.Name)
        {
            string json;
            if (userSearchType == UserSearchType.Name)
                json = await GetAsync($"https://public-ubiservices.ubi.com/v2/profiles?nameOnPlatform={player}&platformType={platform.ToStringValue()}");
            else
                json = await GetAsync($"https://public-ubiservices.ubi.com/v2/users/{player}/profiles?platformType={platform.ToStringValue()}");

            var data = JsonConvert.DeserializeObject<JObject>(json);

            return data["profiles"].ToObject<IEnumerable<Player>>().FirstOrDefault();
        }

        private Dictionary<string, OperatorDef> Operators;
        /// <summary>
        /// Retrieves a list of information about operators - their badge, unique statistic, etc.
        /// </summary>
        /// <returns>List of all operators</returns>
        public async Task<IDictionary<string, OperatorDef>> GetOperatorDefinitions()
        {
            if (Operators is null)
            {
                var json = await GetAsync(OperatorsEndpoint);
                Operators = JsonConvert.DeserializeObject<Dictionary<string, OperatorDef>>(json);
            }
            return Operators;
        }


        private Dictionary<string, WeaponDef> Weapons;
        public async Task<IDictionary<string, WeaponDef>> GetWeaponDefinitions()
        {
            if (Weapons is null)
            {
                var json = await GetAsync(WeaponsEndpoint);
                Weapons = JsonConvert.DeserializeObject<Dictionary<string, WeaponDef>>(json);
            }
            return Weapons;
        }
        /// <summary>
        /// Gets the operators index from the operator definitions dict
        /// </summary>
        /// <param name="name">Name of the operator</param>
        /// <returns>The operator index</returns>
        public async Task<string> GetOperatorIndex(string name)
        {
            return (await GetOperatorDefinitions())[name.ToLower()].Index;
        }

        /// <summary>
        /// Gets the operator unique statistic from the operator definitions dict
        /// </summary>
        /// <param name="name">Name of the operator</param>
        /// <returns>The name of the operator unique statistic</returns>
        public async Task<string> GetOperatorStatistics(string name)
        {
            return (await GetOperatorDefinitions())[name.ToLower()].UniqueStatistics.PvP.StatisticId;
        }

        /// <summary>
        /// Gets the operator badge URL
        /// </summary>
        /// <param name="name">Name of the operator</param>
        /// <returns>The operators badge URL</returns>
        public async Task<string> GetOperatorBadge(string name)
        {
            return (await GetOperatorDefinitions())[name.ToLower()].Badge;
        }

        /// <summary>
        /// Gets the operator
        /// </summary>
        /// <param name="name">Name of the operator</param>
        /// <returns>The operator</returns>
        public async Task<OperatorDef> GetOperator(string name)
        {
            return (await GetOperatorDefinitions())[name.ToLower()];
        }

        private Dictionary<int, Season> Seasons;
        public async Task<Dictionary<int, Season>> GetSeasons()
        {
            if (Seasons is null)
            {
                Seasons = new Dictionary<int, Season>();
                var json = await GetAsync(SeasonsEndpoint);
                var data = JsonConvert.DeserializeObject<JObject>(json);
                Season.LatestSeason = data.Last.First.ToObject<int>();
                foreach (var sjson in data.First.Values())
                {
                    var s = sjson.Last.ToObject<Season>();
                    s.Id = int.Parse(((JProperty)sjson).Name);
                    Seasons.Add(s.Id, s);
                }
            }
            return Seasons;
        }

        private Dictionary<int, string> Locale;
        public async Task<Dictionary<int, string>> GetLocale()
        {
            if (Locale is null)
            {
                var json = await GetAsync(LocaleEndpoint);
                Locale = JsonConvert.DeserializeObject<Dictionary<int, string>>(json);
            }
            return Locale;
        }
    }
}