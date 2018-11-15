using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static R6SiegeAPI.Converters;
using static R6SiegeAPI.Enums;

namespace R6SiegeAPI
{
    public static class Models
    {
        public class Player
        {
            [JsonProperty("profileId")]
            public string ProfileId { get; internal set; }
            [JsonProperty("userId")]
            public string UserId { get; internal set; }
            [JsonProperty("platformType")]
            [JsonConverter(typeof(PlatformConverter))]
            public Platform Platform { get; internal set; }
            public string PlatformUrl => Platform.ToUrlString();
            [JsonProperty("idOnPlatform")]
            public string IdOnPlatform { get; internal set; }
            [JsonProperty("nameOnPlatform")]
            public string Name { get; internal set; }

            public Progression Progression;

            public string Url => $"https://game-rainbow6.ubi.com/en-us/{Platform.ToStringValue()}/player-statistics/{ProfileId}/multiplayer";
            public string IconUrl => $"https://ubisoft-avatars.akamaized.net/{ProfileId}/default_256_256.png";

            private Dictionary<string, Rank> Ranks;
            private Dictionary<string, Operator> Operators;
            private Dictionary<GamemodeNames, GameMode> GameModes;
            private Dictionary<string, Weapon> Weapons;
            private Dictionary<GamemodeQueue, GameQueue> GameQueues;
            private General GeneralStats;
            private General GeneralPvEStats;

            public string SpaceId => API.SpaceIDs[Platform.ToStringValue()];

            private async Task<JToken> FetchStatistics(IEnumerable<string> statistics)
            {
                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/playerstats2/statistics?populations={ProfileId}&statistics={string.Join(',', statistics)}");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                return data["results"][ProfileId];
            }

            private async Task LoadLevel()
            {
                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/r6playerprofile/playerprofile/progressions?profile_ids={ProfileId}");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                var profile = JsonConvert.DeserializeObject<Progression>(data.First.First.First.ToString());
                profile.LootBoxChance /= 10000;
                Progression = profile;
            }

            public async Task<Progression> GetProgression()
            {
                if (Progression is null || Progression.ShouldRefresh)
                    await LoadLevel();
                return Progression;
            }

            private async Task LoadRank(RankedRegion region, int season = -1)
            {
                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/r6karma/players?board_id=pvp_ranked&profile_ids={ProfileId}&region_id={region.ToStringValue()}&season_id={season}");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                var regionkey = $"{region}:{season}";
                if (Ranks is null)
                    Ranks = new Dictionary<string, Rank>();
                Ranks.Add(regionkey, JsonConvert.DeserializeObject<Rank>(data["players"][ProfileId].ToString()));
            }

            public async Task<Rank> GetRank(RankedRegion region, int season = -1)
            {
                if (Ranks is null || !Ranks.ContainsKey($"{region}:{season}") || Ranks[$"{region}:{season}"].ShouldRefresh)
                    await LoadRank(region, season);
                return Ranks[$"{region}:{season}"];
            }

            private async Task LoadAllOperators()
            {
                var statistics = "operatorpvp_kills,operatorpvp_death,operatorpvp_roundwon,operatorpvp_roundlost,operatorpvp_meleekills,operatorpvp_totalxp,operatorpvp_headshot,operatorpvp_timeplayed,operatorpvp_dbno";
                var specifics = string.Join(',', (await API.GetAPI().GetOperatorDefinitions()).Values.Select(x => x.UniqueStatistics.PvP.StatisticId.Split(':')[0]));

                statistics += ',' + specifics;

                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/playerstats2/statistics?populations={ProfileId}&statistics={statistics}");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                var results = data["results"][ProfileId];

                foreach (var opDef in await API.GetAPI().GetOperatorDefinitions())
                {
                    var location = opDef.Value.Index;
                    var values = results.Where(x => x.Path.Contains(location));
                    if (values.Count() > 0)
                    {
                        var opData = '{' + string.Join(',', values.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1].Replace(opDef.Key, "special_stat") + "\":" + x.First)) + '}';
                        var op = JsonConvert.DeserializeObject<Operator>(opData);
                        op.OperatorDefinition = opDef.Value;
                        if (Operators is null)
                            Operators = new Dictionary<string, Operator>();
                        Operators.Add(op.Name, op);
                    }
                }
            }

            public async Task<Dictionary<string, Operator>> GetAllOperators(bool forceRefresh = false)
            {
                if (Operators is null || forceRefresh)
                    await LoadAllOperators();

                return Operators;
            }

            private async Task<Operator> LoadOperator(string opName)
            {
                var location = await API.GetAPI().GetOperatorIndex(opName);

                var opKey = await API.GetAPI().GetOperatorStatistics(opName);
                if (!(opKey is null))
                    opKey = ',' + opKey;
                else
                    opKey = "";

                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/playerstats2/statistics?populations={ProfileId}&statistics=operatorpvp_kills,operatorpvp_death,operatorpvp_roundwon,operatorpvp_roundlost,operatorpvp_meleekills,operatorpvp_totalxp,operatorpvp_headshot,operatorpvp_timeplayed,operatorpvp_dbno{opKey}");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                var results = data["results"][ProfileId];

                var values = results.Where(x => x.Path.Contains(location));
                var opData = '{' + string.Join(',', values.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1].Replace(opName, "special_stat") + "\":" + x.First)) + '}';
                var op = JsonConvert.DeserializeObject<Operator>(opData);
                op.OperatorDefinition = await API.GetAPI().GetOperator(opName);
                if (!(Operators is null))
                    Operators[opName] = op;

                return op;
            }

            public async Task<Operator> GetOperator(string opName)
            {
                if (Operators is null || !Operators.ContainsKey(opName) || Operators[opName].ShouldRefresh)
                    return await LoadOperator(opName);
                else
                    return Operators[opName];
            }

            private async Task LoadWeapons()
            {
                var json = await API.GetAPI().GetAsync($"https://public-ubiservices.ubi.com/v1/spaces/{SpaceId}/sandboxes/{PlatformUrl}/playerstats2/statistics?populations={ProfileId}&statistics=weapontypepvp_kills,weapontypepvp_headshot,weapontypepvp_bulletfired,weapontypepvp_bullethit");
                var data = JsonConvert.DeserializeObject<JObject>(json);

                var results = data["results"][ProfileId];

                foreach (var wepDef in await API.GetAPI().GetWeaponDefinitions())
                {
                    var location = wepDef.Value.Index;
                    var values = results.Where(x => x.Path.Contains($":{location}:"));
                    if (values.Count() > 0)
                    {
                        var wepData = '{' + string.Join(',', values.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1] + "\":" + x.First)) + '}';
                        var wep = JsonConvert.DeserializeObject<Weapon>(wepData);
                        wep.WeaponDefiniton = wepDef.Value;
                        if (Weapons is null)
                            Weapons = new Dictionary<string, Weapon>();
                        Weapons.Add(wep.WeaponDefiniton.Id, wep);
                    }
                }
            }

            public async Task<Dictionary<string, Weapon>> GetWeapons()
            {
                if (Weapons is null || Weapons.Values.ToList().Exists(x => x.ShouldRefresh))
                    await LoadWeapons();

                return Weapons;
            }

            private async Task LoadGamemodes()
            {
                var statistics = new List<string>{ "secureareapvp_matchwon", "secureareapvp_matchlost", "secureareapvp_matchplayed",
                                                   "secureareapvp_bestscore", "rescuehostagepvp_matchwon", "rescuehostagepvp_matchlost",
                                                   "rescuehostagepvp_matchplayed", "rescuehostagepvp_bestscore", "plantbombpvp_matchwon",
                                                   "plantbombpvp_matchlost", "plantbombpvp_matchplayed", "plantbombpvp_bestscore",
                                                   "generalpvp_servershacked", "generalpvp_serverdefender", "generalpvp_serveraggression",
                                                   "generalpvp_hostagerescue", "generalpvp_hostagedefense" };
                var data = await FetchStatistics(statistics);

                for (int i = 0; i <= (int)GamemodeNames.plantbomb; i++)
                {
                    var gameModeEnum = (GamemodeNames)i;
                    var values = data.Where(x => x.Path.Contains(gameModeEnum.ToString()));
                    if (values.Count() > 0)
                    {
                        var gmData = '{' + string.Join(',', values.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1] + "\":" + x.First)) + '}';
                        var gm = JsonConvert.DeserializeObject<GameMode>(gmData);
                        gm.Gamemode = gameModeEnum;
                        if (GameModes is null)
                            GameModes = new Dictionary<GamemodeNames, GameMode>();
                        GameModes.Add(gameModeEnum, gm);
                    }
                }
            }

            public async Task<Dictionary<GamemodeNames, GameMode>> GetGamemodes()
            {
                if (GameModes is null || GameModes.Values.ToList().Exists(x => x.ShouldRefresh))
                    await LoadGamemodes();
                return GameModes;
            }

            private async Task LoadGeneral()
            {
                var statistics = new List<string>{ "generalpvp_timeplayed", "generalpvp_matchplayed", "generalpvp_matchwon",
                                                   "generalpvp_matchlost", "generalpvp_kills", "generalpvp_death",
                                                   "generalpvp_bullethit", "generalpvp_bulletfired", "generalpvp_killassists",
                                                   "generalpvp_revive", "generalpvp_headshot", "generalpvp_penetrationkills",
                                                   "generalpvp_meleekills", "generalpvp_dbnoassists", "generalpvp_suicide",
                                                   "generalpvp_barricadedeployed", "generalpvp_reinforcementdeploy", "generalpvp_totalxp",
                                                   "generalpvp_rappelbreach", "generalpvp_distancetravelled", "generalpvp_revivedenied",
                                                   "generalpvp_dbno", "generalpvp_gadgetdestroy", "generalpvp_blindkills" };
                var data = await FetchStatistics(statistics);
                var gData = '{' + string.Join(',', data.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1] + "\":" + x.First)) + '}';
                GeneralStats = JsonConvert.DeserializeObject<General>(gData);
            }

            public async Task<General> GetGeneral()
            {
                if (GeneralStats is null || GeneralStats.ShouldRefresh)
                    await LoadGeneral();
                return GeneralStats;
            }

            private async Task LoadGeneralPvE()
            {
                var statistics = new List<string>{ "generalpve_timeplayed", "generalpve_matchplayed", "generalpve_matchwon",
                                                   "generalpve_matchlost", "generalpve_kills", "generalpve_death",
                                                   "generalpve_bullethit", "generalpve_bulletfired", "generalpve_killassists",
                                                   "generalpve_revive", "generalpve_headshot", "generalpve_penetrationkills",
                                                   "generalpve_meleekills", "generalpve_dbnoassists", "generalpve_suicide",
                                                   "generalpve_barricadedeployed", "generalpve_reinforcementdeploy", "generalpve_totalxp",
                                                   "generalpve_rappelbreach", "generalpve_distancetravelled", "generalpve_revivedenied",
                                                   "generalpve_dbno", "generalpve_gadgetdestroy", "generalpve_blindkills" };
                var data = await FetchStatistics(statistics);
                var gData = '{' + string.Join(',', data.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1] + "\":" + x.First)) + '}';
                GeneralPvEStats = JsonConvert.DeserializeObject<General>(gData);
            }

            public async Task<General> GetGeneralPvE()
            {
                if (GeneralPvEStats is null || GeneralPvEStats.ShouldRefresh)
                    await LoadGeneralPvE();
                return GeneralPvEStats;
            }

            private async Task LoadQueues()
            {
                var statistics = new List<string>{ "casualpvp_matchwon", "casualpvp_matchlost", "casualpvp_timeplayed",
                                                   "casualpvp_matchplayed", "casualpvp_kills", "casualpvp_death",
                                                   "rankedpvp_matchwon", "rankedpvp_matchlost", "rankedpvp_timeplayed",
                                                   "rankedpvp_matchplayed", "rankedpvp_kills", "rankedpvp_death" };
                var data = await FetchStatistics(statistics);

                for (int i = 0; i <= (int)GamemodeQueue.Casual; i++)
                {
                    var gameModeEnum = (GamemodeQueue)i;
                    var values = data.Where(x => x.Path.Contains(gameModeEnum.ToString().ToLower()));
                    if (values.Count() > 0)
                    {
                        var gmData = '{' + string.Join(',', values.Select(x => "\"" + x.Path.Split(':')[0].Split('_')[1] + "\":" + x.First)) + '}';
                        var gm = JsonConvert.DeserializeObject<GameQueue>(gmData);
                        gm.Gamemode = gameModeEnum;
                        if (GameQueues is null)
                            GameQueues = new Dictionary<GamemodeQueue, GameQueue>();
                        GameQueues.Add(gameModeEnum, gm);
                    }
                }
            }

            public async Task<Dictionary<GamemodeQueue, GameQueue>> GetQueues()
            {
                if (GameQueues is null || GameQueues.Values.ToList().Exists(x => x.ShouldRefresh))
                    await LoadQueues();
                return GameQueues;
            }
        }

        public class OperatorDef
        {
            [JsonProperty("id")]
            public string Id { get; internal set; }
            [JsonProperty("category")]
            [JsonConverter(typeof(OperatorCategoryConverter))]
            public OperatorCategory Category { get; internal set; }
            [JsonProperty("name")]
            public Oasis Name { get; internal set; }
            [JsonProperty("ctu")]
            public Oasis Ctu { get; internal set; }
            [JsonProperty("index")]
            public string Index { get; internal set; }
            [JsonProperty("figure")]
            public Figure Figure { get; internal set; }
            [JsonProperty("mask")]
            public string Mask { get; internal set; }
            [JsonProperty("badge")]
            public string Badge { get; internal set; }
            [JsonProperty("uniqueStatistic")]
            public UniqueStatistics UniqueStatistics { get; internal set; }
        }

        public class Operator : Refreshable
        {
            [JsonIgnore]
            public string Name => OperatorDefinition.Name.ToString();
            [JsonProperty("roundwon")]
            public int Wins { get; internal set; }
            [JsonProperty("roundlost")]
            public int Losses { get; internal set; }
            [JsonProperty("kills")]
            public int Kills { get; internal set; }
            [JsonProperty("death")]
            public int Deaths { get; internal set; }
            [JsonProperty("headshot")]
            public int Headshots { get; internal set; }
            [JsonProperty("meleekills")]
            public int Melees { get; internal set; }
            [JsonProperty("dbno")]
            public int DBNOs { get; internal set; }
            [JsonProperty("totalxp")]
            public int XP { get; internal set; }
            [JsonProperty("timeplayed")]
            [JsonConverter(typeof(TimeSpanFromSecondsConverter))]
            public TimeSpan TimePlayed { get; internal set; }
            [JsonProperty("special_stat")]
            public int Statistic { get; internal set; }
            [JsonIgnore]
            public string StatisticName => OperatorDefinition.UniqueStatistics.PvP.Label.ToString();
            [JsonIgnore]
            public OperatorDef OperatorDefinition;
        }

        public class Oasis
        {
            [JsonProperty("oasisId")]
            public int OasisId { get; internal set; }
            public async Task<string> GetValue()
            {
                var locale = await API.GetAPI().GetLocale();
                if (locale.ContainsKey(OasisId))
                    return locale[OasisId];
                else
                    return OasisId.ToString();
            }

            public override string ToString()
            {
                var t = GetValue();
                t.Wait();
                return t.Result;
            }
        }

        public class Figure
        {
            [JsonProperty("small")]
            public string Small { get; internal set; }
            [JsonProperty("large")]
            public string Large { get; internal set; }
        }

        public class UniqueStatistics
        {
            [JsonProperty("pvp")]
            public Statistic PvP { get; internal set; }
            [JsonProperty("pve")]
            public Statistic PvE { get; internal set; }
        }

        public class Statistic
        {
            [JsonProperty("statisticId")]
            public string StatisticId { get; internal set; }
            [JsonProperty("label")]
            public Oasis Label { get; internal set; }
        }

        public class Rank : Refreshable
        {
            public static List<string> Ranks = new List<string>{
                "Unranked",
                "Copper 4",   "Copper 3",   "Copper 2",   "Copper 1",
                "Bronze 4",   "Bronze 3",   "Bronze 2",   "Bronze 1",
                "Silver 4",   "Silver 3",   "Silver 2",   "Silver 1",
                "Gold 4",     "Gold 3",     "Gold 2",     "Gold 1",
                "Platinum 3", "Platinum 2", "Platinum 1", "Diamond"
            };

            public static List<string> RankCharms = new List<string>
            {
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20copper%20charm.44c1ede2.png",
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20bronze%20charm.5edcf1c6.png",
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20silver%20charm.adde1d01.png",
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20gold%20charm.1667669d.png",
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20platinum%20charm.d7f950d5.png",
                "https://ubistatic-a.akamaihd.net/0058/prod/assets/images/season02%20-%20diamond%20charm.e66cad88.png"
            };

            public static List<string> RankIcons = new List<string>
            {
                "https://i.imgur.com/sB11BIz.png",  // unranked
                "https://i.imgur.com/ehILQ3i.jpg",  // copper 4
                "https://i.imgur.com/6CxJoMn.jpg",  // copper 3
                "https://i.imgur.com/eI11lah.jpg",  // copper 2
                "https://i.imgur.com/0J0jSWB.jpg",  // copper 1
                "https://i.imgur.com/42AC7RD.jpg",  // bronze 4
                "https://i.imgur.com/QD5LYD7.jpg",  // bronze 3
                "https://i.imgur.com/9AORiNm.jpg",  // bronze 2
                "https://i.imgur.com/hmPhPBj.jpg",  // bronze 1
                "https://i.imgur.com/D36ZfuR.jpg",  // silver 4
                "https://i.imgur.com/m8GToyF.jpg",  // silver 3
                "https://i.imgur.com/EswGcx1.jpg",  // silver 2
                "https://i.imgur.com/KmFpkNc.jpg",  // silver 1
                "https://i.imgur.com/6Qg6aaH.jpg",  // gold 4
                "https://i.imgur.com/B0s1o1h.jpg",  // gold 3
                "https://i.imgur.com/ELbGMc7.jpg",  // gold 2
                "https://i.imgur.com/ffDmiPk.jpg",  // gold 1
                "https://i.imgur.com/Sv3PQQE.jpg",  // plat 3
                "https://i.imgur.com/Uq3WhzZ.jpg",  // plat 2
                "https://i.imgur.com/xx03Pc5.jpg",  // plat 1
                "https://i.imgur.com/nODE0QI.jpg"   // diamond
            };

            public static Bracket BracketFromRank(int RankId)
            {
                if (RankId == 0) return 0;
                else if (RankId <= 4) return (Bracket)1;
                else if (RankId <= 8) return (Bracket)2;
                else if (RankId <= 12) return (Bracket)3;
                else if (RankId <= 16) return (Bracket)4;
                else if (RankId <= 19) return (Bracket)5;
                else return (Bracket)6;
            }

            [JsonProperty("max_mmr")]
            public float MaxMMR { get; internal set; }
            [JsonProperty("mmr")]
            public float MMR { get; internal set; }
            [JsonProperty("wins")]
            public int Wins { get; internal set; }
            [JsonProperty("losses")]
            public int Losses { get; internal set; }
            [JsonProperty("abandons")]
            public int Abandons { get; internal set; }
            [JsonProperty("rank")]
            public int RankId { get; internal set; }
            [JsonProperty("max_rank")]
            public int MaxRank { get; internal set; }
            [JsonProperty("next_rank_mmr")]
            public float NextRankMMR { get; internal set; }
            [JsonProperty("season")]
            [JsonConverter(typeof(SeasonConverter))]
            public Season Season { get; internal set; }
            [JsonProperty("region")]
            [JsonConverter(typeof(RegionConverter))]
            public RankedRegion Region { get; internal set; }
            [JsonProperty("skill_mean")]
            public float SkillMean { get; internal set; }
            [JsonProperty("skill_stdev")]
            public float SkillStDev { get; internal set; }

            [JsonIgnore]
            public string RankName => Ranks[RankId];
            [JsonIgnore]
            public string GetIconUrl => RankIcons[RankId];
            public string GetCharmUrl()
            {
                if (RankId <= 4) return RankCharms[0];
                else if (RankId <= 8) return RankCharms[1];
                else if (RankId <= 12) return RankCharms[2];
                else if (RankId <= 16) return RankCharms[3];
                else if (RankId <= 19) return RankCharms[4];
                else return RankCharms[5];
            }
            [JsonIgnore]
            public Bracket GetBracket => BracketFromRank(RankId);
        }

        public class Weapon : Refreshable
        {
            [JsonIgnore]
            public string Name => WeaponDefiniton.Name.ToString();
            [JsonIgnore]
            public WeaponDef WeaponDefiniton;

            [JsonProperty("kills")]
            public int Kills { get; set; }
            [JsonProperty("headshot")]
            public int Headshonts { get; set; }
            [JsonProperty("bullethit")]
            public int Hits { get; set; }
            [JsonProperty("bulletfired")]
            public int Shots { get; set; }
        }

        public abstract class Game : Refreshable
        {
            [JsonProperty("matchwon")]
            public int Won { get; internal set; }
            [JsonProperty("matchlost")]
            public int Lost { get; internal set; }
            [JsonProperty("matchplayed")]
            public int Played { get; internal set; }
        }

        public class GameMode : Game
        {
            [JsonIgnore]
            public GamemodeNames Gamemode { get; set; }
            [JsonIgnore]
            public string Name => Gamemode.ToStringValue();

            [JsonProperty("bestscore")]
            public int BestScore { get; internal set; }
        }

        public class GameQueue : Game
        {
            [JsonIgnore]
            public GamemodeQueue Gamemode { get; internal set; }
            [JsonIgnore]
            public string Name => Gamemode.ToString();

            [JsonProperty("timeplayed")]
            [JsonConverter(typeof(TimeSpanFromSecondsConverter))]
            public TimeSpan TimePlayed { get; set; }
            [JsonProperty("kills")]
            public int Kills { get; set; }
            [JsonProperty("death")]
            public int Deaths { get; set; }
        }

        public class Progression : Refreshable
        {
            [JsonProperty("xp")]
            public int XP { get; internal set; }
            [JsonProperty("lootbox_probability")]
            public float LootBoxChance { get; internal set; }
            [JsonProperty("level")]
            public int Level { get; internal set; }
        }

        public class Season
        {
            [JsonIgnore]
            public static int LatestSeason { get; set; }
            [JsonIgnore]
            public int Id { get; internal set; }
            [JsonProperty("name")]
            public string Name { get; internal set; }
            [JsonProperty("background")]
            public string Background { get; internal set; }
        }

        public class WeaponDef
        {
            [JsonProperty("id")]
            public string Id { get; internal set; }
            [JsonProperty("name")]
            public Oasis Name { get; internal set; }
            [JsonProperty("index")]
            public int Index { get; internal set; }
        }

        public class General : Refreshable
        {
            [JsonProperty("timeplayed")]
            [JsonConverter(typeof(TimeSpanFromSecondsConverter))]
            public TimeSpan TimePlayed { get; internal set; }
            [JsonProperty("matchplayed")]
            public int MatchPlayed { get; internal set; }
            [JsonProperty("matchwon")]
            public int MatchWon { get; internal set; }
            [JsonProperty("matchlost")]
            public int MatchLost { get; internal set; }
            [JsonProperty("kills")]
            public int Kills { get; internal set; }
            [JsonProperty("death")]
            public int Deaths { get; internal set; }
            [JsonProperty("bullethit")]
            public int BulletHits { get; internal set; }
            [JsonProperty("bulletfired")]
            public int BulletFired { get; internal set; }
            [JsonProperty("killassists")]
            public int KillAssists { get; internal set; }
            [JsonProperty("revive")]
            public int Revives { get; internal set; }
            [JsonProperty("headshot")]
            public int Headshots { get; internal set; }
            [JsonProperty("penetrationkills")]
            public int PenetrationKills { get; internal set; }
            [JsonProperty("meleekills")]
            public int MeleeKills { get; internal set; }
            [JsonProperty("dbnoassists")]
            public int DBNOAssists { get; internal set; }
            [JsonProperty("suicide")]
            public int Sucicides { get; internal set; }
            [JsonProperty("barricadedeployed")]
            public int BarricadesDeployed { get; internal set; }
            [JsonProperty("reinforcementdeploy")]
            public int ReinforcementsDeployed { get; internal set; }
            [JsonProperty("totalxp")]
            public int TotalXP { get; internal set; }
            [JsonProperty("rappelbreach")]
            public int RappelBreach { get; internal set; }
            [JsonProperty("distancetravelled")]
            public int DistanceTravelled { get; internal set; }
            [JsonProperty("revivedenied")]
            public int RevivesDenied { get; internal set; }
            [JsonProperty("dbno")]
            public int DBNOs { get; internal set; }
            [JsonProperty("gadgetdestroy")]
            public int GadgetsDestroyed { get; internal set; }
            [JsonProperty("blindkills")]
            public int BlindKills { get; internal set; }
        }

        public abstract class Refreshable
        {
            private DateTime createdAt = DateTime.Now;
            public bool ShouldRefresh => (DateTime.Now - createdAt) > TimeSpan.FromMinutes(15);
        }
    }
}
