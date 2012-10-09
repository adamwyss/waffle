using System.Diagnostics;
using System;

namespace WaFFL.Evaluation
{
    [DebuggerDisplay("{TeamCode}")]
    [Serializable]
    public class NFLTeam
    {
        public NFLTeam(string code)
        {
            this.TeamCode = code;
        }

        public string TeamCode { get; private set; }

        public ESPNDefenseLeaders ESPNTeamDefense { get; set; }
    }
}
