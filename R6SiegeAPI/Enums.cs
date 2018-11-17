using System;
using System.Collections.Generic;
using System.Text;
using static R6SiegeAPI.Enums;

namespace R6SiegeAPI
{
    static class EnumsExtensions
    {
        public static string ToStringValue(this RankedRegion region)
        {
            switch (region)
            {
                case RankedRegion.EU:
                    return "emea";
                case RankedRegion.NA:
                    return "ncsa";
                case RankedRegion.ASIA:
                    return "apac";
                default:
                    throw new ArgumentOutOfRangeException("Region");
            }
        }

        public static string ToStringValue(this Platform platform)
        {
            switch (platform)
            {
                case Platform.UPLAY:
                    return "uplay";
                case Platform.XBOX:
                    return "xbn";
                case Platform.PLAYSTATION:
                    return "psn";
                case Platform.STEAM:
                    return "steam";
                default:
                    throw new ArgumentOutOfRangeException("Platform");
            }
        }

        public static string ToStringValue(this GamemodeNames gamemode)
        {
            switch (gamemode)
            {
                case GamemodeNames.securearea:
                    return "Secure Area";
                case GamemodeNames.rescuehostage:
                    return "Hostage Rescue";
                case GamemodeNames.plantbomb:
                    return "Bomb";
                default:
                    throw new ArgumentOutOfRangeException("Platform");
            }
        }

        public static string ToUrlString(this Platform platform)
        {
            switch (platform)
            {
                case Platform.UPLAY:
                    return "OSBOR_PC_LNCH_A";
                case Platform.XBOX:
                    return "OSBOR_PS4_LNCH_A";
                case Platform.PLAYSTATION:
                    return "OSBOR_XBOXONE_LNCH_A";
                default:
                    throw new ArgumentOutOfRangeException("Platform");
            }
        }

        public static string ToStringValue(this Locales locale)
        {
            return locale.ToString().Replace('_', '-');
        }
    }

    public static class Enums
    {
        public enum RankedRegion
        {
            EU,
            NA,
            ASIA
        }

        public enum Platform
        {
            UPLAY,
            XBOX,
            PLAYSTATION,
            STEAM
        }

        public enum GamemodeNames
        {
            securearea,
            rescuehostage,
            plantbomb
        }

        public enum GamemodeQueue
        {
            Ranked,
            Casual
        }

        public enum Locales
        {
            cs_cz,
            de_de,
            en_au,
            en_gb,
            en_nordic,
            en_us,
            es_es,
            es_mx,
            fr_ca,
            fr_fr,
            it_it,
            ja_jp,
            ko_kr,
            nl_nl,
            pl_pl,
            pt_br,
            ru_ru,
            zh_cn,
            zh_tw
        }

        /// <summary>
        /// The type of search to perform when doing a user search
        /// </summary>
        public enum UserSearchType
        {
            /// <summary>
            /// Searching using name
            /// </summary>
            Name,
            /// <summary>
            /// Searching using uid
            /// </summary>
            UId
        }

        public enum OperatorCategory
        {
            Defense,
            Attack
        }

        public enum Bracket
        {
            Unranked = 0,
            Copper = 1,
            Bronze = 2,
            Silver = 3,
            Gold = 4,
            Platinum = 5,
            Diamond = 6
        }
    }
}
