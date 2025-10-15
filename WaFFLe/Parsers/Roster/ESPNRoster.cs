using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class ESPNRoster : IWaFFLRoster
    {
        private static readonly Dictionary<string, string> mappings = new Dictionary<string, string>()
        {
            { "Arizona Cardinals", "Cardinals D/ST" },
            { "Atlanta Falcons", "Falcons D/ST" },
            { "Baltimore Ravens", "Ravens D/ST" },
            { "Buffalo Bills", "Bills D/ST" },
            { "Carolina Panthers", "Panthers D/ST" },
            { "Cincinnati Bengals", "Bengals D/ST" },
            { "Chicago Bears", "Bears D/ST" },
            { "Cleveland Browns", "Browns D/ST" },
            { "Dallas Cowboys", "Cowboys D/ST" },
            { "Denver Broncos", "Broncos D/ST" },
            { "Detroit Lions", "Lions D/ST" },
            { "Green Bay Packers", "Packers D/ST" },
            { "Houston Texans", "Texans D/ST" },
            { "Indianapolis Colts", "Colts D/ST" },
            { "Jacksonville Jaguars", "Jaguars D/ST" },
            { "Kansas City Chiefs", "Chiefs D/ST" },
            { "Los Angeles Chargers", "Chargers D/ST" },
            { "Los Angeles Rams", "Rams D/ST" },
            { "Las Vegas Raiders", "Raiders D/ST" },
            { "Miami Dolphins", "Dolphins D/ST" },
            { "Minnesota Vikings", "Vikings D/ST" },
            { "New England Patriots", "Patriots D/ST" },
            { "New Orleans Saints", "Saints D/ST" },
            { "New York Giants", "Giants D/ST" },
            { "New York Jets", "Jets D/ST" },
            { "Philadelphia Eagles", "Eagles D/ST" },
            { "Pittsburgh Steelers", "Steelers D/ST" },
            { "Seattle Seahawks", "Seahawks D/ST" },
            { "San Francisco 49ers", "49ers D/ST" },
            { "Tampa Bay Buccaneers", "Buccaneers D/ST" },
            { "Tennessee Titans", "Titans D/ST" },
            { "Washington Commanders", "Commanders D/ST" },

        //  { "ProfootballReference Spelling", "ESPN Spelling" },
            { "D.J. Moore", "DJ Moore" },
            { "Travis Etienne", "Travis Etienne Jr." },
            { "D.K. Metcalf", "DK Metcalf" },
            { "Darrell Henderson", "Darrell Henderson Jr." },
            //2025
            { "James Cook", "James Cook III" },
        };

        /// <summary />
        private const string WaFFLRosterUri = @"https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/seasons/2025/segments/0/leagues/69765935?view=mSettings&view=mRoster&view=mTeam&view=modular&view=mNav";

        /// <summary />
        private Dictionary<string, string> playerLookup;

        /// <summary />
        public ESPNRoster()
        {
            this.playerLookup = new Dictionary<string, string>();

            WebClient client = new WebClient();
            string json = client.DownloadString(WaFFLRosterUri);

            JObject doc = JObject.Parse(json);
            foreach (var team in doc["teams"])
            {
                var teamCode = team["abbrev"].ToString();
                foreach (var r in team["roster"]["entries"])
                {
                    var player = r["playerPoolEntry"]["player"];
                    var name = player["fullName"].ToString();

                    this.playerLookup.Add(name, teamCode);
                }
            }

        }

        /// <summary />
        public string CheckRosterStatus(string search)
        {
            //some players are coming in with a space.  need to check enjestion.
            var trimmed = search.Trim();
            if (trimmed != search)
            {
                if (Debugger.IsAttached)
                {
                    throw new InvalidOperationException();
                }
                System.Diagnostics.Debug.WriteLine("ESPNRoster Found spaces on search term '{0}', expected: '{1}'", search, trimmed);
                search = trimmed;
            }

            string alternate;
            bool alternateExists = mappings.TryGetValue(search, out alternate);
            if (alternateExists)
            {
                search = alternate;
            }

            string teamCode;
            bool found = this.playerLookup.TryGetValue(search, out teamCode);
            if (!found)
            {
                return null;
            }

            return teamCode;
        }
    }
}
