using System.Linq;
using System.Collections.Generic;
using System;

namespace WaFFL.Evaluation
{
    /// <summary />
    [Serializable]
    public class FanastySeason
    {
        /// <summary />
        private Dictionary<int, NFLPlayer> playerCache = new Dictionary<int, NFLPlayer>();

        /// <summary />
        private Dictionary<string, NFLTeam> teamCache = new Dictionary<string, NFLTeam>();

        /// <summary />
        public FanastySeason()
        {
        }

        /// <summary />
        public int Year { get; set; }

        /// <summary />
        public DateTime LastUpdated { get; set; }

        /// <summary />
        public PositionBaseline ReplacementValue { get; set; }

        /// <summary />
        public NFLPlayer GetPlayer(int uuid)
        {
            NFLPlayer player;

            bool exists = this.playerCache.TryGetValue(uuid, out player);
            if (!exists)
            {
                // create the player and update the cache
                player = new NFLPlayer(uuid);
                this.playerCache.Add(uuid, player);
            }

            return player;
        }

        /// <summary />
        public bool IsBadTeamCode(string code)
        {
            code = code.TrimStart('@');
            code = code.TrimStart('v', 's');
            code = code.TrimStart();
            code = code.TrimEnd();

            return code == "IRV" || code == "CTR" || code == "RIC" || string.IsNullOrEmpty(code);
        }

        /// <summary />
        public NFLTeam GetTeam(string code)
        {
            code = code.TrimStart('@');
            code = code.TrimStart('v', 's');
            code = code.TrimStart();
            code = code.TrimEnd();
            
            NFLTeam team;

            bool exists = this.teamCache.TryGetValue(code, out team);
            if (!exists)
            {
                // create the team and update the cache
                team = new NFLTeam(code);
                this.teamCache.Add(code, team);
            }

            return team;
        }

        public int GetWeek(string date, int year)
        {
            int week = 0;

            if (year == 2016)
            {
                switch (date)
                {
                    case "Thu 9/8":
                    case "Sun 9/11":
                    case "Mon 9/12":
                        week = 1;
                        break;
                    case "Thu 9/18":
                    case "Sun 9/18":
                    case "Mon 9/19":
                        week = 2;
                        break;
                    case "Thu 9/22":
                    case "Sun 9/25":
                    case "Mon 9/26":
                        week = 3;
                        break;
                    case "Thu 9/29":
                    case "Sun 10/2":
                    case "Mon 10/3":
                        week = 4;
                        break;
                    case "Thu 10/6":
                    case "Sun 10/9":
                    case "Mon 10/10":
                        week = 5;
                        break;
                    case "Thu 10/13":
                    case "Sun 10/16":
                    case "Mon 10/17":
                        week = 6;
                        break;
                    case "Thu 10/20":
                    case "Sun 10/23":
                    case "Mon 10/24":
                        week = 7;
                        break;
                    case "Thu 10/27":
                    case "Sun 10/30":
                    case "Mon 10/31":
                        week = 8;
                        break;
                    case "Thu 11/3":
                    case "Sun 11/6":
                    case "Mon 11/7":
                        week = 9;
                        break;
                    case "Thu 11/10":
                    case "Sun 11/13":
                    case "Mon 11/14":
                        week = 10;
                        break;
                    case "Thu 11/17":
                    case "Sun 11/20":
                    case "Mon 11/21":
                        week = 11;
                        break;
                    case "Thu 11/24":
                    case "Sun 11/27":
                    case "Mon 11/28":
                        week = 12;
                        break;
                    case "Thu 12/1":
                    case "Sun 12/4":
                    case "Mon 12/5":
                        week = 13;
                        break;
                    case "Thu 12/8":
                    case "Sun 12/11":
                    case "Mon 12/12":
                        week = 14;
                        break;
                    case "Thu 12/15":
                    case "Sat 12/17":
                    case "Sun 12/18":
                    case "Mon 12/19":
                        week = 15;
                        break;
                    case "Thu 12/22":
                    case "Sat 12/24":
                    case "Sun 12/25":
                    case "Mon 12/26":
                        week = 16;
                        break;
                    case "Sun 1/1":
                        week = 17;
                        break;
                    default:
                        week = 0;
                        break;
                }
            }
             else if (year == 2017)
             {
                switch (date)
                {
                   case "Thu 9/7":
                   case "Sun 9/10":
                   case "Mon 9/11":
                      week = 1;
                      break;
                   case "Thu 9/14":
                   case "Sun 9/17":
                   case "Mon 9/18":
                      week = 2;
                      break;
                   case "Thu 9/21":
                   case "Sun 9/24":
                   case "Mon 9/25":
                      week = 3;
                      break;
                   case "Thu 9/28":
                   case "Sun 10/1":
                   case "Mon 10/2":
                      week = 4;
                      break;
                   case "Thu 10/5":
                   case "Sun 10/8":
                   case "Mon 10/9":
                      week = 5;
                      break;
                   case "Thu 10/11":
                   case "Sun 10/15":
                   case "Mon 10/16":
                      week = 6;
                      break;
                   case "Thu 10/19":
                   case "Sun 10/22":
                   case "Mon 10/23":
                      week = 7;
                      break;
                   case "Thu 10/26":
                   case "Sun 10/29":
                   case "Mon 10/30":
                      week = 8;
                      break;
                   case "Thu 11/2":
                   case "Sun 11/5":
                   case "Mon 11/6":
                      week = 9;
                      break;
                   case "Thu 11/9":
                   case "Sun 11/12":
                   case "Mon 11/13":
                      week = 10;
                      break;
                   case "Thu 11/16":
                   case "Sun 11/19":
                   case "Mon 11/20":
                      week = 11;
                      break;
                   case "Thu 11/23":
                   case "Sun 11/26":
                   case "Mon 11/27":
                      week = 12;
                      break;
                   case "Thu 11/30":
                   case "Sun 12/3":
                   case "Mon 12/4":
                      week = 13;
                      break;
                   case "Thu 12/7":
                   case "Sun 12/10":
                   case "Mon 12/11":
                      week = 14;
                      break;
                   case "Thu 12/14":
                   case "Sat 12/16":
                   case "Sun 12/17":
                   case "Mon 12/18":
                      week = 15;
                      break;
                   case "Sat 12/23":
                   case "Sun 12/24":
                   case "Mon 12/25":
                      week = 16;
                      break;
                   case "Sun 12/31":
                      week = 17;
                      break;
                   default:
                      week = 0;
                      break;
                }
             }

            else if (year == 2018)
            {
                switch (date)
                {
                    case "Thu 9/6":
                    case "Sun 9/9":
                    case "Mon 9/10":
                        week = 1;
                        break;
                    case "Thu 9/13":
                    case "Sun 9/16":
                    case "Mon 9/17":
                        week = 2;
                        break;
                    case "Thu 9/20":
                    case "Sun 9/23":
                    case "Mon 9/24":
                        week = 3;
                        break;
                    case "Thu 9/27":
                    case "Sun 9/30":
                    case "Mon 10/1":
                        week = 4;
                        break;
                    case "Thu 10/4":
                    case "Sun 10/7":
                    case "Mon 10/8":
                        week = 5;
                        break;
                    case "Thu 10/11":
                    case "Sun 10/14":
                    case "Mon 10/15":
                        week = 6;
                        break;
                    case "Thu 10/18":
                    case "Sun 10/21":
                    case "Mon 10/22":
                        week = 7;
                        break;
                    case "Thu 10/25":
                    case "Sun 10/28":
                    case "Mon 10/29":
                        week = 8;
                        break;
                    case "Thu 11/1":
                    case "Sun 11/4":
                    case "Mon 11/5":
                        week = 9;
                        break;
                    case "Thu 11/8":
                    case "Sun 11/11":
                    case "Mon 11/12":
                        week = 10;
                        break;
                    case "Thu 11/15":
                    case "Sun 11/18":
                    case "Mon 11/19":
                        week = 11;
                        break;
                    case "Thu 11/22":
                    case "Sun 11/25":
                    case "Mon 11/26":
                        week = 12;
                        break;
                    case "Thu 11/29":
                    case "Sun 12/2":
                    case "Mon 12/3":
                        week = 13;
                        break;
                    case "Thu 12/6":
                    case "Sun 12/9":
                    case "Mon 12/10":
                        week = 14;
                        break;
                    case "Thu 12/13":
                    case "Sat 12/15":
                    case "Sun 12/16":
                    case "Mon 12/17":
                        week = 15;
                        break;
                    case "Sun 12/23":
                    case "Mon 12/24":
                        week = 16;
                        break;
                    case "Sun 12/30":
                        week = 17;
                        break;
                    default:
                        week = 0;
                        break;
                }
            }

            return week;
        }

        /// <summary />
        public IEnumerable<NFLPlayer> GetAll()
        {
            var query = from p in this.playerCache.Values
                        select p;
            return query.ToArray();
        }

        /// <summary />
        public IEnumerable<NFLPlayer> GetAll(params FanastyPosition[] positions)
        {
            var cache = new HashSet<FanastyPosition>(positions);

            var query = from p in this.playerCache.Values
                        where cache.Contains(p.Position)
                        select p;
            return query.ToArray();
        }


        /// <summary />
        public IEnumerable<NFLPlayer> GetAllPlayers()
        {
            return this.playerCache.Values;
        }

        /// <summary />
        public IEnumerable<NFLTeam> GetAllTeams()
        {
            return this.teamCache.Values;
        }
    }
}
