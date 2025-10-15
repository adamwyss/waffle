using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        private Dictionary<string, NFLPlayer> nameIndex;

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

        /// <summary>
        /// Finds player by ID or creates a new player reference with the specified initialization.
        /// </summary>
        public NFLPlayer GetPlayer(string uuid, Action<NFLPlayer> initializer = null)
        {
            NFLPlayer player;

            bool exists = this.playerCache.TryGetValue(uuid, out player);
            if (!exists && initializer != null)
            {
                // create the player and update the cache
                player = new NFLPlayer(uuid);
                this.playerCache.Add(uuid, player);
                initializer(player);
            }

            return player;
        }

        /// <summary />
        public IEnumerable<NFLPlayer> GetAllPlayers()
        {
            var query = from p in this.playerCache.Values
                        select p;
            return query.ToArray();
        }

        /// <summary />
        public IEnumerable<NFLPlayer> GetAll(params FanastyPosition[] positions)
        {
            var cache = new HashSet<FanastyPosition>(positions);

            var query = from p in this.GetAllPlayers()
                        where cache.Contains(p.GetPositionOrBestGuess())
                        select p;
            return query.ToArray();
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
        public IEnumerable<NFLTeam> GetAllTeams()
        {
            var query = from t in this.teamCache.Values
                        select t;
            return query.ToArray();
        }

        /// <summary />
        public void ClearAllPlayerGameLogs()
        {
            foreach (NFLPlayer player in this.playerCache.Values)
            {
                player.GameLog.Clear();
            }
        }

        /// <summary />
        public void ClearAllPlayerGameLogs(int week)
        {
            foreach (NFLPlayer player in this.playerCache.Values)
            {
                var games = player.GameLog.Where(x => x.Week == week).ToArray();
                foreach (Game g in games)
                {
                    player.GameLog.Remove(g);
                }
            }
        }

        /// <summary />
        public void CreateIndex()
        {
            if (this.nameIndex != null)
            {
                throw new InvalidOperationException("Index already exists.  Call DeleteIndex before CreateIndex.");
            }

            this.nameIndex = this.playerCache.Values.ToDictionary(p => p.Name);
        }

        /// <summary />
        public void DeleteIndex()
        {
            this.nameIndex = null;
        }

        /// <summary />
        public NFLPlayer GetPlayerByIndex(string name)
        {
            if (this.nameIndex == null)
            {
                throw new InvalidOperationException("Index does not exist.  Call CreateIndex before GetPlayerByIndex.");
            }

            NFLPlayer player;
            bool exists = this.nameIndex.TryGetValue(name, out player);
            if (!exists)
            {
                // apparently a player can score, but never be in the listing...
                // at this point, we don't have and id, page url or anything but a name.
                // create an empty element, but don't add it to the season.
                return new NFLPlayer("fake");
                throw new InvalidOperationException("Player with specified name does not exist.");
            }

            return player;
        }
    }
}
