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
        public NFLTeam GetTeam(string code)
        {
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

            if (year == 2010)
            {
                switch (date)
                {
                    case "Thu 9/9":
                    case "Sun 9/12":
                    case "Mon 9/13":
                        week = 1;
                        break;
                    case "Sun 9/19":
                    case "Mon 9/20":
                        week = 2;
                        break;
                    case "Sun 9/26":
                    case "Mon 9/27":
                        week = 3;
                        break;
                    case "Sun 10/3":
                    case "Mon 10/4":
                        week = 4;
                        break;
                    case "Sun 10/10":
                    case "Mon 10/11":
                        week = 5;
                        break;
                    case "Sun 10/17":
                    case "Mon 10/18":
                        week = 6;
                        break;
                    case "Sun 10/24":
                    case "Mon 10/25":
                        week = 7;
                        break;
                    case "Sun 10/31":
                    case "Mon 11/1":
                        week = 8;
                        break;
                    case "Sun 11/7":
                    case "Mon 11/8":
                        week = 9;
                        break;
                    case "Thu 11/11":
                    case "Sun 11/14":
                    case "Mon 11/15":
                        week = 10;
                        break;
                    case "Thu 11/18":
                    case "Sun 11/21":
                    case "Mon 11/22":
                        week = 11;
                        break;
                    case "Thu 11/25":
                    case "Sun 11/28":
                    case "Mon 11/29":
                        week = 12;
                        break;
                    case "Thu 12/2":
                    case "Sun 12/5":
                    case "Mon 12/6":
                        week = 13;
                        break;
                    case "Thu 12/9":
                    case "Sun 12/12":
                    case "Mon 12/13":
                        week = 14;
                        break;
                    case "Thu 12/16":
                    case "Sun 12/19":
                    case "Mon 12/20":
                        week = 15;
                        break;
                    case "Thu 12/23":
                    case "Sat 12/25":
                    case "Sun 12/26":
                    case "Mon 12/27":
                    case "Tue 12/28":
                        week = 16;
                        break;
                    case "Sun 1/2":
                        week = 17;
                        break;
                    default:
                        week = 0;
                        break;
                }
            }
            else if (year == 2011)
            {
                switch (date)
                {
                    case "Thu 9/8":
                    case "Sun 9/11":
                    case "Mon 9/12":
                        week = 1;
                        break;
                    case "Sun 9/18":
                    case "Mon 9/19":
                        week = 2;
                        break;
                    case "Sun 9/25":
                    case "Mon 9/26":
                        week = 3;
                        break;
                    case "Sun 10/2":
                    case "Mon 10/3":
                        week = 4;
                        break;
                    case "Sun 10/9":
                    case "Mon 10/10":
                        week = 5;
                        break;
                    case "Sun 10/16":
                    case "Mon 10/17":
                        week = 6;
                        break;
                    case "Sun 10/23":
                    case "Mon 10/24":
                        week = 7;
                        break;
                    case "Sun 10/30":
                    case "Mon 10/31":
                        week = 8;
                        break;
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
            else if (year == 2012)
            {
                switch (date)
                {
                    case "Wed 9/5":
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
                    case "Sun 12/16":
                    case "Mon 12/17":
                        week = 15;
                        break;
                    case "Sat 12/22":
                    case "Sun 12/23":
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

            return week;
        }

        /// <summary />
        public IEnumerable<NFLPlayer> GetAll(FanastyPosition position)
        {
            if (position == FanastyPosition.DST)
            {
                throw new InvalidOperationException();
            }

            var query = from p in this.playerCache.Values
                        where p.Position == position
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
