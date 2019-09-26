using System;
using System.Diagnostics;

namespace WaFFL.Evaluation
{
    /// <summary />
    [DebuggerDisplay("Week {Week} vs {Opponent}")]
    [Serializable]
    public class Game
    {
        /// <summary />
        public int Week { get; set; }

        /// <summary />
        public NFLTeam Opponent { get; set; }

        /// <summary />
        public Passing Passing { get; set; }

        /// <summary />
        public Rushing Rushing { get; set; }

        /// <summary />
        public Receiving Receiving { get; set; }

        /// <summary />
        public Fumbles Fumbles { get; set; }

        /// <summary />
        public Kicking Kicking { get; set; }

        /// <summary />
        public Defense Defense { get; set; }
    }


    /// <summary />
    [DebuggerDisplay("P: {YDS}yds, {INT}int")]
    [Serializable]
    public class Passing
    {
        /// <summary>
        /// Passes Completed
        /// </summary>
        public int CMP { get; set; }

        /// <summary>
        /// Passes Attempted
        /// </summary>
        public int ATT { get; set; }

        /// <summary>
        /// Yards gained by passing (
        /// </summary>
        public int YDS { get; set; }

        /// <summary>
        /// Passing Touchdowns
        /// </summary>
        public int TD { get; set; }

        /// <summary>
        /// Interceptions Thrown
        /// </summary>
        public int INT { get; set; }

        /// <summary>
        /// Longest Completion
        /// </summary>
        public int LONG { get; set; }
    }

    /// <summary />
    [DebuggerDisplay("R: {YDS}yds")]
    [Serializable]
    public class Rushing
    {
        /// <summary>
        /// Rushing Attempts (sacks not included in NFL)
        /// </summary>
        public int CAR { get; set; }

        /// <summary>
        /// Rushing Yards Gained (sacks not included in NFL)
        /// </summary>
        public int YDS { get; set; }

        /// <summary>
        /// Rushing Touchdowns
        /// </summary>
        public int TD { get; set; }

        /// <summary>
        /// Longest Rushing Attempt
        /// </summary>
        public int LONG { get; set; }
    }

    /// <summary />
    [DebuggerDisplay("C: {YDS}yds")]
    [Serializable]
    public class Receiving
    {
        /// <summary>
        /// Receptions
        /// </summary>
        public int REC { get; set; }

        /// <summary>
        /// Receiving Yards
        /// </summary>
        public int YDS { get; set; }

        /// <summary>
        /// Receiving Touchdowns
        /// </summary>
        public int TD { get; set; }

        /// <summary>
        /// Longest Reception
        /// </summary>
        public int LONG { get; set; }
    }

    /// <summary />
    [DebuggerDisplay("FUM: {FUM}fum")]
    [Serializable]
    public class Fumbles
    {
        /// <summary>
        /// Fumbles
        /// </summary>
        public int FUM { get; set; }

        /// <summary>
        /// Fumbles Lost
        /// </summary>
        public int LOST { get; set; }
    }

    /// <summary />
    [DebuggerDisplay("K: {XPM}XP; {FGM_01to19+FGM_20to29+FGM_30to39+FGM_40to49+FGM_50plus}FG")]
    [Serializable]
    public class Kicking
    {
        /// <summary>
        /// 1-19 yard field goals made
        /// </summary>
        public int FGM_01to19 { get; set; }

        /// <summary>
        /// 1-19 yard field goals attempts
        /// </summary>
        public int FGA_01to19 { get; set; }

        /// <summary>
        /// 20-29 yard field goals made
        /// </summary>
        public int FGM_20to29 { get; set; }

        /// <summary>
        /// 20-29 yard field goals attempts
        /// </summary>
        public int FGA_20to29 { get; set; }

        /// <summary>
        /// 30-39 yard field goals made
        /// </summary>
        public int FGM_30to39 { get; set; }

        /// <summary>
        /// 30-39 yard field goals attempts
        /// </summary>
        public int FGA_30to39 { get; set; }

        /// <summary>
        /// 40-49 yard field goals made
        /// </summary>
        public int FGM_40to49 { get; set; }

        /// <summary>
        /// 40-49 yard field goals attempts
        /// </summary>
        public int FGA_40to49 { get; set; }

        /// <summary>
        /// 50+ yard field goals made
        /// </summary>
        public int FGM_50plus { get; set; }

        /// <summary>
        /// 50+ yard field goals attempts
        /// </summary>
        public int FGA_50plus { get; set; }

        /// <summary>
        /// Longest field goal made
        /// </summary>
        public int LONG { get; set; }

        /// <summary>
        /// Extra points made
        /// </summary>
        public int XPM { get; set; }

        /// <summary>
        /// Extra point attempts
        /// </summary>
        public int XPA { get; set; }
    }

    /// <summary />
    [DebuggerDisplay("DST: {SACK}s, {INT}int, {REC}fum")]
    [Serializable]
    public class Defense
    {
        /// <summary>
        /// Total sacks
        /// </summary>
        public int SACK { get; set; }

        /// <summary>
        /// Passes intercepted
        /// </summary>
        public int INT { get; set; }

        /// <summary>
        /// Intercepted returned yards
        /// </summary>
        public int YDS { get; set; }

        /// <summary>
        /// Interceptions returned for touchdowns
        /// </summary>
        public int TD_INT { get; set; }

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
