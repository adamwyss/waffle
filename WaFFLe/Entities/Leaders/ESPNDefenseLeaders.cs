using System.Diagnostics;
using System;

namespace WaFFL.Evaluation
{
    /// <summary />
    [DebuggerDisplay("DST: {SACK}s, {INT}int, {REC}fum")]
    [Serializable]
    public class ESPNDefenseLeaders
    {
        /// <summary>
        /// Unassisted tackles
        /// </summary>
        public int SOLO { get; set; }

        /// <summary>
        /// Assisted tackles
        /// </summary>
        public int AST { get; set; }

        /// <summary>
        /// Total sacks
        /// </summary>
        public int SACK { get; set; }

        /// <summary>
        /// Yards lost on sack
        /// </summary>
        public int YDSL { get; set; }

        /// <summary>
        /// Pass defended
        /// </summary>
        public int PD { get; set; }

        /// <summary>
        /// Passes intercepted
        /// </summary>
        public int INT { get; set; }

        /// <summary>
        /// Intercepted returned yards
        /// </summary>
        public int YDS { get; set; }

        /// <summary>
        /// Longest interception return
        /// </summary>
        public int LONG { get; set; }

        /// <summary>
        /// Interceptions returned for touchdowns
        /// </summary>
        public int TD_INT { get; set; }

        /// <summary>
        /// Forced fumbles
        /// </summary>
        public int FF { get; set; }

        /// <summary>
        /// Fumbles recovered
        /// </summary>
        public int REC { get; set; }

        /// <summary>
        /// Fumbles returned for touchdowns
        /// </summary>
        public int TD_FUM { get; set; }
    }
}
