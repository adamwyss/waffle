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
        private Dictionary<string, NFLPlayer> playerCache = new Dictionary<string, NFLPlayer>();

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

        public NFLPlayer GetPlayer(string uuid, Action<NFLPlayer> initializer = null)
        {
            NFLPlayer player;

            bool exists = this.playerCache.TryGetValue(uuid, out player);
            if (!exists)
            {
                // create the player and update the cache
                player = new NFLPlayer(uuid);
                this.playerCache.Add(uuid, player);
                if (initializer != null)
                {
                    initializer(player);
                }
            }

            return player;
        }

        /// <summary />
        public NFLTeam GetTeam(string code)
        {
            code = code.Trim();
            
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

        /// <summary />
        public IEnumerable<NFLPlayer> GetAllPlayers()
        {
            var query = from p in this.playerCache.Values
                        where p.Position != FanastyPosition.UNKNOWN
                        select p;
            return query.ToArray();
        }

        public void ClearAllPlayerGameLogs()
        {
            foreach (NFLPlayer player in this.playerCache.Values)
            {
                player.GameLog.Clear();
            }
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
        public IEnumerable<NFLTeam> GetAllTeams()
        {
            return this.teamCache.Values;
        }
    }
}
