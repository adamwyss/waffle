using System.Diagnostics;
using System.Collections.ObjectModel;
using System;

namespace WaFFL.Evaluation
{
    [DebuggerDisplay("{Name}, {Position} ({Team.TeamCode})")]
    [Serializable]
    public class NFLPlayer
    {
        public NFLPlayer(string uuid)
        {
            this.GameLog = new Collection<Game>();
            this.Identifier = uuid;
        }

        public string Identifier { get; private set; }

        public string PlayerPageUri { get; set; }

        public string Name { get; set; }

        public FanastyPosition Position { get; set; }

        public NFLTeam Team { get; set; }

        public Collection<Game> GameLog { get; private set; }
    }
}
