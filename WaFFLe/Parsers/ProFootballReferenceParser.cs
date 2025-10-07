
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Xml.Linq;

namespace WaFFL.Evaluation
{
    public class ProFootballReferenceParser
    {
        /// <summary />
        private readonly string SeasonScheduleUri = "https://www.pro-football-reference.com/years/{0}/games.htm";

        /// <summary />
        private readonly string InjuryReportUri = "https://www.pro-football-reference.com/players/injuries.htm";

        /// <summary />
        private readonly string[] Delimiters = new string[] { "\n" };

        /// <summary />
        private WebClient httpClient;

        /// <summary />
        private FanastySeason context;

        /// <summary />
        private Action<string> callback;

        /// <summary />
        private int webRequests;

        /// <summary />
        public ProFootballReferenceParser(FanastySeason season, Action<string> callback)
        {
            this.context = season;
            this.callback = callback;
            this.httpClient = new WebClient();
            this.webRequests = 0;
        }

        /// <summary />
        public void ParseWeek(int week)
        {
            bool validWeek = week > 0 && week <= 17;
            if (!validWeek)
            {
                throw new InvalidOperationException("invalid week: " + week.ToString());
            }

            try
            {
                this.context.ClearAllPlayerGameLogs(week);

                string uri = string.Format(SeasonScheduleUri, this.context.Year);
                ParseGames(uri, week);
                ParseInjuries(InjuryReportUri);
            }
            finally
            {
                this.context.LastUpdated = DateTime.Now;
            }
        }

        private void ParseGames(string uri, int targetWeek)
        {
            string xhtml = this.ClientDownloadString(uri);
            var games = this.ExtractRawGames(xhtml);
            foreach (var game in games)
            {
                bool continueParsing = this.ParseGame(game, uri, targetWeek);
                if (!continueParsing)
                {
                    break;
                }
            }
        }

        private bool ParseGame(XElement game, string baseUri, int targetWeek)
        {
            var fields = game.Elements().ToList();
            string week = fields[0].Value;

            string targetWeekString = targetWeek.ToString(CultureInfo.InvariantCulture);
            if (week != targetWeekString)
            {
                // continue to next week
                return true;
            }

            string winner = fields[4].Value;
            string location = fields[5].Value;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = "vs";
            }
            string loser = fields[6].Value;

            TeamsInGame teamsInGame = new TeamsInGame(this.context, TeamConverter.ConvertToCode(winner), TeamConverter.ConvertToCode(loser));

            this.callback(string.Format("Week {0} - {1} {2} {3}", week, winner, location, loser));

            var boxscore = fields[7];
            bool completed = boxscore.Value.Equals("boxscore");
            if (completed)
            {
                string uri = boxscore.Element("a").Attribute("href").Value;
                string boxscoreUri = new Uri(new Uri(baseUri), uri).AbsoluteUri;
                string xhtml = this.ClientDownloadString(boxscoreUri);

                var players = ExtractPlayerOffense(xhtml);
                var kickers = ExtractPlayerKicking(xhtml);
                var op = new PlayerParser(this.context, int.Parse(week), teamsInGame);
                op.RecordAllPlayerStats(players);
                op.RecordKickingStats(kickers);

                var defensivePlayers = ExtractDefense(xhtml);
                var teamStats = ExtractTeamStats(xhtml);
                var dp = new DefenseParser(this.context, int.Parse(week), teamsInGame);
                dp.RecordInterceptions(defensivePlayers);
                dp.RecordSacksAndFumbles(teamStats);

                this.context.CreateIndex();
                var scores = ExtractScoringPlays(xhtml);
                var spp = new ScoringPlayParser(this.context, int.Parse(week));
                spp.RecordScoringPlays(scores);
                this.context.DeleteIndex();

            }

            return completed;
        }

        private List<XElement> ExtractDefense(string xhtml)
        {
            const string start = "    <table class=\"sortable stats_table shade_zero\" id=\"player_defense\" data-cols-to-freeze=\",1\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractTeamStats(string xhtml)
        {
            const string start = "    <table class=\"add_controls stats_table\" id=\"team_stats\" data-cols-to-freeze=\",1\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            List<XElement> list = new List<XElement>();
            list.Add(parsedElement.Elements("thead").Single().Element("tr"));
            list.AddRange(parsedElement.Elements("tr"));

            return list;
        }

        private List<XElement> ExtractPlayerKicking(string xhtml)
        {
            const string start = "    <table class=\"sortable stats_table\" id=\"kicking\" data-cols-to-freeze=\",1\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractPlayerOffense(string xhtml)
        {
            const string start = "    <table class=\"sortable stats_table\" id=\"player_offense\" data-cols-to-freeze=\",1\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractScoringPlays(string xhtml)
        {
            const string start = "    <table class=\"stats_table\" id=\"scoring\" data-cols-to-freeze=\"1,2\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var scores = parsedElement.Elements("tr")
                                      .ToList();
            return scores;
        }

        private bool IsPlayerRow(XElement row)
        {
            var classAttribute = row.Attribute("class");
            if (classAttribute != null)
            {
                return !classAttribute.Value.Equals("thead") && !classAttribute.Value.Equals("over_header thead");
            }

            return true;
        }

        private List<XElement> ExtractRawGames(string xhtml)
        {
            const string start = "    <table class=\"sortable stats_table\" id=\"games\" data-cols-to-freeze=\"1,3\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var games = parsedElement.Elements("tr")
                                     .Where(IsGameRow)
                                     .ToList();
            return games;
        }

        private bool IsGameRow(XElement row)
        {
            var classAttribute = row.Attribute("class");
            if (classAttribute != null)
            {
                return !classAttribute.Value.Equals("thead");
            }

            return true;
        }

        private void ParseInjuries(string uri)
        {
            ClearPlayerInjuries();

            // fetch injury status.
            string xhtml = this.ClientDownloadString(uri);
            var injuries = ExtractInjuryReport(xhtml);
            foreach (var injury in injuries)
            {
                ParseInjury(injury);
            }
        }

        private void ClearPlayerInjuries()
        {
            foreach (var player in context.GetAllPlayers())
            {
                player.Status = null;
            }
        }

        private void ParseInjury(XElement injury)
        {
            var fields = injury.Elements().ToList();
            var id = fields[0].Attribute("data-append-csv").Value;
            var player = this.context.GetPlayer(id, null);
            if (player != null)
            {
                var status = fields[3].Value;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var report = fields[4].Value;
                    var pratice = fields[5].Value;

                    player.Status = new InjuryStatus()
                    {
                        Status = ConvertToInjuryStatus(status),
                        Reason = string.Format("{0} - {1}", report, pratice)
                    };
                }
            }
        }

        private PlayerInjuryStatus ConvertToInjuryStatus(string status)
        {
            switch (status)
            {
                case "Out":
                    return PlayerInjuryStatus.Out;
                case "Doubtful":
                    return PlayerInjuryStatus.Doubtful;
                case "Questionable":
                    return PlayerInjuryStatus.Questionable;
                case "Unknown":
                    return PlayerInjuryStatus.Probable;
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                throw new InvalidOperationException();
            }

            return PlayerInjuryStatus.Probable;
        }

        private List<XElement> ExtractInjuryReport(string xhtml)
        {
            const string start = "    <table class=\"sortable stats_table\" id=\"injuries\" data-cols-to-freeze=\"1,2\">";
            const string end = "</table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var injuries = parsedElement.Elements("tr")
                                     .ToList();
            return injuries;
        }

        private string ClientDownloadString(string uri)
        {
            
            string filePath = null;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                string cacheDirectory = "cache";

                if (!Directory.Exists(cacheDirectory))
                { 
                    Directory.CreateDirectory(cacheDirectory);
                }

                string hash = ComputeSha256Hash(uri);
                filePath = Path.Combine(cacheDirectory, $"{hash}.html");

                DateTime lastWrite = File.GetLastWriteTimeUtc(filePath);
                bool oneHourCache = (uri.EndsWith("games.htm") || uri == InjuryReportUri);
                bool cacheExpired = (DateTime.UtcNow - lastWrite) > TimeSpan.FromHours(1);
                if (File.Exists(filePath) && (!oneHourCache || (oneHourCache && !cacheExpired)))
                {
                    Console.WriteLine("Using Cache {0} - {1}", ++this.webRequests, uri);
                    return File.ReadAllText(filePath);
                }
                else if (oneHourCache && cacheExpired)
                    Console.WriteLine("Cache Expired - {0}", lastWrite);
            }
            
            // we are somewhat restricted on our web requests.
            System.Threading.Thread.Sleep(new Random().Next(2000, 5000));
            string data = this.httpClient.DownloadString(uri);
            
            Console.WriteLine("Web Requests {0} - {1}", ++this.webRequests, uri);


            if (System.Diagnostics.Debugger.IsAttached && filePath != null)
            {
                File.WriteAllText(filePath, data);
            }

            return data;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();

                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }

        private XElement ExtractRawData(string xhtml, string startingline, string endingline, string[] exclude, bool optional = false, Func<string, string> cleanupDelegate = null)
        {
            // extract the html data table that we are interested in.
            string[] lines = xhtml.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == startingline)
                {
                    if (sb != null)
                    {
                        throw new InvalidOperationException("two instances of data entry point found.");
                    }

                    // start logging the lines
                    sb = new StringBuilder();
                }

                if (sb != null && !exclude.Contains(lines[i]))
                {
                    // remember the lines.
                    sb.AppendLine(lines[i]);
                }

                if (sb != null && lines[i] == endingline)
                {
                    // do some post processing
                    var raw = sb.ToString();
                    raw = raw.Replace("&nbsp;", " ");
                    raw = raw.Replace("& ", "&amp; ");
                    raw = raw.Replace("<br>", " ");
                    raw = raw.Replace("<br />", " ");
                    raw = raw.Replace("<tbody>", "");
                    raw = raw.Replace("data-tip=\"<b>Yards per Punt</b>", "data-tip=\"Yards per Punt");
                    raw = raw.Replace("</td></td>", "</td>");

                    raw = raw.Replace("</div><!-- div.media-item --><div >", "<div>");
                    raw = raw.Replace(", <a href='https://www.pro-football-reference.com/about/glossary.htm'>see glossary</a> for details Different ratings are used by the NFL and NCAA. Minimum 1500 pass attempts to qualify as career leader, minimum 150 pass attempts for playoffs leader.", "");

                    if (cleanupDelegate != null)
                    {
                        raw = cleanupDelegate(raw);
                    }

                    // parse and return
                    return XElement.Parse(raw);
                }
            }

            if (optional)
            {
                return null;
            }

            throw new InvalidOperationException("The specified data was not found");
        }
    }

    public class TeamsInGame
    {
        NFLTeam team1;
        NFLTeam team2;

        public TeamsInGame(FanastySeason season, string code1, string code2)
            : this(season.GetTeam(code1), season.GetTeam(code2))
        {
        }

        public TeamsInGame(NFLTeam team1, NFLTeam team2)
        {
            this.team1 = team1;
            this.team2 = team2;
        }

        public NFLTeam GetOther(NFLTeam team)
        {
            if (this.team1 == team)
                return this.team2;
            if (this.team2 == team) 
                return this.team1;
            throw new InvalidOperationException();
        }
    }

    public class PlayerParser
    {
        private readonly FanastySeason _season;
        private readonly int _week;
        private readonly TeamsInGame _teamsInGame;

        public PlayerParser(FanastySeason season, int week, TeamsInGame teamsInGame)
        {
            _season = season;
            _week = week;
            _teamsInGame = teamsInGame;
        }

        public void RecordAllPlayerStats(List<XElement> players)
        {
            foreach (var player in players)
            {
                if (player.Elements().ToList().First().Attribute("data-append-csv") == null)
                {
                    // boxscore contains player stat line with no player id - we are missing stats
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        throw new Exception("Invalid player in boxscore");
                    }
                }

                this.RecordPlayerStats(player);
            }
        }
        
        public void RecordKickingStats(List<XElement> kickers)
        {
            foreach (var kicker in kickers)
            {
                this.RecordKickerStats(kicker);
            }
        }

        private void RecordPlayerStats(XElement element)
        {
            NFLPlayer player = ExtractPlayerInfo(element);
            AssignTeam(player, element);

            var values = element.Elements().ToList();
            Game game = new Game();
            game.Week = this._week;
            game.Opponent = this._teamsInGame.GetOther(player.Team);

            int attempts = int.Parse(values[3].Value);
            if (attempts > 0)
            {
                Passing p = game.Passing = new Passing();
                p.CMP = int.Parse(values[2].Value);
                p.ATT = attempts;
                p.YDS = int.Parse(values[4].Value);
                p.LONG = int.Parse(values[9].Value);
                p.TD = int.Parse(values[5].Value);
                p.INT = int.Parse(values[6].Value);
            }

            int carries = int.Parse(values[11].Value);
            if (carries > 0)
            {
                Rushing r = game.Rushing = new Rushing();
                r.CAR = carries;
                r.YDS = int.Parse(values[12].Value);
                r.LONG = int.Parse(values[14].Value);
                r.TD = int.Parse(values[13].Value);
            }

            int targets = int.Parse(values[15].Value);
            if (targets > 0)
            {
                Receiving c = game.Receiving = new Receiving();
                c.REC = int.Parse(values[16].Value);
                c.YDS = int.Parse(values[17].Value);
                c.LONG = int.Parse(values[19].Value);
                c.TD = int.Parse(values[18].Value);
            }

            Fumbles f = game.Fumbles = new Fumbles();
            f.FUM = int.Parse(values[20].Value);
            f.LOST = int.Parse(values[21].Value);

            player.GameLog.Add(game);
        }


        private void RecordKickerStats(XElement element)
        {
            var values = element.Elements().ToList();
            string rawXPM = values[2].Value;
            string rawXPA = values[3].Value;
            string rawFGM = values[4].Value;
            string rawFGA = values[5].Value;

            if (string.IsNullOrWhiteSpace(rawXPM) && string.IsNullOrWhiteSpace(rawXPA) &&
                string.IsNullOrWhiteSpace(rawFGM) && string.IsNullOrWhiteSpace(rawFGA))
            {
                // punters and kickers are in the same table, if no values are present, then
                // this is a punter.
                return;
            }

            NFLPlayer player = ExtractPlayerInfo(element);
            AssignTeam(player, element);

            Game game = player.GameLog.SingleOrDefault(g => g.Week == this._week);
            if (game == null)
            {
                game = new Game();
                game.Week = this._week;
                game.Opponent = this._teamsInGame.GetOther(player.Team);
                player.GameLog.Add(game);
            }

            Kicking k = game.Kicking = new Kicking();

            k.FGM = string.IsNullOrWhiteSpace(rawFGM) ? 0 : int.Parse(rawFGM);
            k.FGA = string.IsNullOrWhiteSpace(rawFGA) ? 0 : int.Parse(rawFGA);

            k.XPM = string.IsNullOrWhiteSpace(rawXPM) ? 0 : int.Parse(rawXPM);
            k.XPA = string.IsNullOrWhiteSpace(rawXPA) ? 0 : int.Parse(rawXPA);
        }

        private NFLPlayer ExtractPlayerInfo(XElement element)
        {
            var fields = element.Elements().ToList();
            string playerid = fields[0].Attribute("data-append-csv").Value;
            string name = fields[0].Value.Trim();

            // the player must have an id and name, but position is optional
            if (playerid == null || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException();
            }

            NFLPlayer player = this._season.GetPlayer(playerid, p =>
            {
                p.PlayerPageUri = fields[0].Element("a")?.Attribute("href")?.Value;
                p.Name = name;
            });

            if (player.Name != name)
            {
                Console.WriteLine("Updating players name '{0}' -> '{1}'.", player.Name, name);
                player.Name = name;
            }

            return player;
        }

        private void AssignTeam(NFLPlayer player, XElement element)
        {
            // if the player has played for multiple teams, we 
            // only want the most recent team that they have 
            // played for.
            var fields = element.Elements().ToList();
            string teamcode = fields[1].Value;

            NFLTeam team = this._season.GetTeam(teamcode);
            if (player.Team != team)
            {
                // if team has changed, we will update -- we only need to track the most recent
                // team.  This is all that matters.
                player.Team = team;
            }
        }
    }

    public class DefenseParser
    {
        private readonly FanastySeason _season;
        private readonly int _week;
        private readonly TeamsInGame _teamsInGame;

        public DefenseParser(FanastySeason season, int week, TeamsInGame teamsInGame)
        {
            _season = season;
            _week = week;
            _teamsInGame = teamsInGame;
        }

        public void RecordInterceptions(List<XElement> element)
        {
            var teams = element.GroupBy(e => e.Elements().ToList()[1].Value);
            foreach (var team in teams)
            {
                var teamCode = team.Key;

                int interceptions = 0;
                int interceptionYards = 0;
                int interceptionTouchdowns = 0;

                foreach (var te in team)
                {
                    var values = te.Elements().ToList();

                    interceptions += Convert.ToInt32(values[2].Value);
                    interceptionYards += Convert.ToInt32(values[3].Value);
                    interceptionTouchdowns += Convert.ToInt32(values[4].Value);
                }

                Defense dst = GetOrCreateGameDefense(teamCode);
                dst.INT = interceptions;
                dst.YDS_INT = interceptionYards;
                dst.TD_INT = interceptionTouchdowns;
            }
        }

        public void RecordSacksAndFumbles(List<XElement> teamStats)
        {
            var header = teamStats[0].Elements("th").ToList();
            var teamCode1 = header[1].Attribute("aria-label").Value;
            var teamCode2 = header[2].Attribute("aria-label").Value;

            var fumbles = ParseTeamStatsRow(teamStats[7], "Fumbles-Lost", 1);
            var sacked = ParseTeamStatsRow(teamStats[4], "Sacked-Yards", 0);

            // Stats are recorded as offensive fumbles lost, we need
            // defensive fumble recovered, so we will swap the team
            // and values.

            // this shoudl be called after defensive, so D game long
            // SHOULD Be populated.

            Defense dst1 = GetOrCreateGameDefense(teamCode1);
            dst1.FUM = fumbles.Team2;
            dst1.SACK = sacked.Team2;

            Defense dst2 = GetOrCreateGameDefense(teamCode2);
            dst2.FUM = fumbles.Team1;
            dst2.SACK = sacked.Team1;
        }

        private Defense GetOrCreateGameDefense(string teamCode)
        {
            NFLPlayer player = this._season.GetPlayer(teamCode, p =>
            {
                p.PlayerPageUri = string.Format("/teams/{0}/{1}.htm", teamCode.ToLowerInvariant(), this._season.Year);
                p.Team = this._season.GetTeam(teamCode);
                p.Name = TeamConverter.ConvertToName(teamCode);
                p.Position = FanastyPosition.DST;
            });

            Game game = player.GameLog.SingleOrDefault(g => g.Week == this._week);
            if (game == null)
            {
                game = new Game();
                player.GameLog.Add(game);
                game.Week = this._week;
                game.Opponent = this._teamsInGame.GetOther(player.Team);
            }

            if (game.Defense == null)
            {
                game.Defense = new Defense();
            }

            return game.Defense;
        }

        private StatValues ParseTeamStatsRow(XElement row, string headerText, int index)
        {

            var header = row.Elements("th").Single().Value;
            if (header != headerText)
            {
                throw new Exception("Parser did not find header: " + headerText);
            }

            var rawValues = row.Elements("td").ToList();
            var team1Values = rawValues[0].Value;
            var team2Values = rawValues[1].Value;

            var team1Value = team1Values.Split('-')[index];
            var team2Value = team2Values.Split('-')[index];

            return new StatValues {
                        Team1 = int.Parse(team1Value),
                        Team2 = int.Parse(team2Value)
                    };
        }

        private class StatValues
        {
            public int Team1 { get; set; }
            public int Team2 { get; set; }
        }
    }

    public class ScoringPlayParser
    {
        public delegate void ParseHandler(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values);

        // scoring types that should have an extra point (not all do)
        private static readonly Dictionary<Regex, ParseHandler> tdscores = new Dictionary<Regex, ParseHandler>()
        {
            // td passes
            { new Regex(@"([\.\w- ']*) (\d*) yard pass from ([\.\w- ']*)", RegexOptions.Compiled), ScoringPlayHandler.TouchdownPass },

            // td rushing
            { new Regex(@"([\.\w- ']*) (\d*) yard rush", RegexOptions.Compiled), ScoringPlayHandler.TouchdownRun },

            // td defense
            { new Regex(@"([\.\w- ']*) fumble recovery in end zone", RegexOptions.Compiled), ScoringPlayHandler.EndzoneRecovery },
            { new Regex(@"([\.\w- ']*) kickoff recovery in end zone", RegexOptions.Compiled), ScoringPlayHandler.EndzoneRecovery },
            { new Regex(@"([\.\w- ']*) (\d*) yard fumble return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownDefense },
            { new Regex(@"([\.\w- ']*) (\d*) yard interception return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownDefense },
            { new Regex(@"([\.\w- ']*) (\d*) yard blocked punt return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownSpecialTeams },
            { new Regex(@"([\.\w- ']*) (\d*) yard blocked field goal return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownSpecialTeams },
            { new Regex(@"([\.\w- ']*) (\d*) yard kickoff return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownSpecialTeams },
            { new Regex(@"([\.\w- ']*) (\d*) yard punt return", RegexOptions.Compiled), ScoringPlayHandler.TouchdownSpecialTeams },
            //Markquese Bell defensive extra point return
        };

        // extra point types
        private static readonly Dictionary<Regex, ParseHandler> xp = new Dictionary<Regex, ParseHandler>()
        {
            // extra points
            { new Regex(@"\(([\.\w-Ã± ']*) kick\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\.\w-Ã± ']*) kick failed\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\.\w- ']*) run\)$", RegexOptions.Compiled), ScoringPlayHandler.ExtraPointRun },
            { new Regex(@"\(run failed\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\.\w- ']*) pass from ([\.\w- ']*)\)$", RegexOptions.Compiled), ScoringPlayHandler.ExtraPointPass },
            { new Regex(@"\(pass failed\)$", RegexOptions.Compiled), null },
        };

        // scoring types that do not have an extra point
        private static readonly Dictionary<Regex, ParseHandler> otherscores = new Dictionary<Regex, ParseHandler>()
        {
            // field goals
            { new Regex(@"([\.\w-Ã± ']*) (\d*) yard field goal", RegexOptions.Compiled), ScoringPlayHandler.FieldGoal },

            // safety
            { new Regex(@"Safety, ([\.\w- ']*) tackled in end zone by ([\w ']*)", RegexOptions.Compiled), ScoringPlayHandler.Safety },
            { new Regex(@"Safety, ([\.\w- ']*) sacked in end zone by ([\w ']*)", RegexOptions.Compiled), ScoringPlayHandler.Safety },
            { new Regex(@"Safety, ([\.\w- ']*) offensive holding in end zone", RegexOptions.Compiled), ScoringPlayHandler.Safety },
        };

        private readonly FanastySeason _season;
        private readonly int _week;

        public ScoringPlayParser(FanastySeason season, int week)
        {
            _season = season;
            _week = week;
        }

        public void RecordScoringPlays(List<XElement> scores)
        {
            foreach (var score in scores)
            {
                string scoringPlay = score.Elements().ToList()[3].Value;

                // capture team for defensive scores
                string teamName = score.Elements().ToList()[2].Value;
                NFLPlayer team = _season.GetAll(FanastyPosition.DST).SingleOrDefault(p => p.Name.EndsWith(teamName));

                Process(scoringPlay, team);
            }
        }

        private bool Process(string scoringPlay, NFLPlayer team)
        {
            bool success = TryMatchingScoringHandler(tdscores, scoringPlay, team, () =>
            {
                bool xpsuccess = TryMatchingScoringHandler(xp, scoringPlay, team);
                if (!xpsuccess)
                {
                    // happens when a TD wins the game
                    Console.WriteLine("NO-XP FOUND: {0}", scoringPlay);
                }
            });
            
            if (success)
            {
                return true;
            }

            success = TryMatchingScoringHandler(otherscores, scoringPlay, team);

            if (success)
            {
                return true;
            }

            Console.WriteLine("UNKNOWN-SCORE: {0}", scoringPlay);
            return false;
        }

        private bool TryMatchingScoringHandler(Dictionary<Regex, ParseHandler> actions, string text, NFLPlayer scoringTeam, Action followup = null)
        {
            foreach (var set in actions)
            {
                Regex re = set.Key;
                Match match = re.Match(text);
                if (match.Success)
                {
                    int count = match.Groups.Count;
                    string[] captures = new string[count - 1];
                    for (int i = 1; i < count; i++)
                    {
                        captures[i - 1] = match.Groups[i].Value.Trim();
                    }

                    set.Value?.Invoke(_season, _week, scoringTeam, captures);
                    followup?.Invoke();
                    return true;
                }
            }

            return false;
        }
    }

    public static class ScoringPlayHandler
    {
        public static ScoringPlayParser.ParseHandler TouchdownPass = new ScoringPlayParser.ParseHandler(HandleTouchdownPass);
        public static ScoringPlayParser.ParseHandler TouchdownRun = new ScoringPlayParser.ParseHandler(HandleTouchdownRun);
        public static ScoringPlayParser.ParseHandler FieldGoal = new ScoringPlayParser.ParseHandler(HandleFieldGoal);
        public static ScoringPlayParser.ParseHandler ExtraPointRun = new ScoringPlayParser.ParseHandler(HandleExtraPointRun);
        public static ScoringPlayParser.ParseHandler ExtraPointPass = new ScoringPlayParser.ParseHandler(HandleExtraPointPass);
        public static ScoringPlayParser.ParseHandler TouchdownDefense = new ScoringPlayParser.ParseHandler(HandleTouchdownDefense);
        public static ScoringPlayParser.ParseHandler TouchdownSpecialTeams = new ScoringPlayParser.ParseHandler(HandleTouchdownSpecialTeams);
        public static ScoringPlayParser.ParseHandler Safety = new ScoringPlayParser.ParseHandler(HandleSafety);
        public static ScoringPlayParser.ParseHandler EndzoneRecovery = new ScoringPlayParser.ParseHandler(HandleEndzoneRecovery);

        private static void HandleTouchdownPass(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 3);

            string receiverName = values[0];
            int distance = int.Parse(values[1]);
            string passerName = values[2];

            var reciever = FindPlayerByName(season, receiverName);
            var c = GetOrCreateGameReceiving(reciever, week);
            AddTouchdownYards(c, distance);

            var passer = FindPlayerByName(season, passerName);
            var p = GetOrCreateGamePassing(passer, week);
            AddTouchdownYards(p, distance);
        }

        private static void HandleTouchdownRun(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 2);

            string name = values[0];
            int distance = int.Parse(values[1]);

            var rusher = FindPlayerByName(season, name);
            var r = GetOrCreateGameRushing(rusher, week);
            AddTouchdownYards(r, distance);
        }

        private static void HandleFieldGoal(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 2);

            string name = values[0];
            int distance = int.Parse(values[1]);

            var kicker = FindPlayerByName(season, name);
            var k = GetOrCreateGameKicking(kicker, week);
            AddFieldGoalYards(k, distance);
        }

        private static void HandleExtraPointRun(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 1);

            string name = values[0];

            var rusher = FindPlayerByName(season, name);
            var r = GetOrCreateGameRushing(rusher, week);
            r.TWO_PT_CONV++;
        }

        private static void HandleExtraPointPass(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 2);

            string receiverName = values[0];
            string passerName = values[1];

            var reciever = FindPlayerByName(season, receiverName);
            var c = GetOrCreateGameReceiving(reciever, week);
            c.TWO_PT_CONV++;

            var passer = FindPlayerByName(season, passerName);
            var p = GetOrCreateGamePassing(passer, week);
            p.TWO_PT_CONV++;
        }

        private static void HandleTouchdownDefense(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 2);

            string player = values[0];
            int distance = int.Parse(values[1]);

            var d = GetOrCreateGameDefense(scoringTeam, week);
            AddTouchdownYards(d, distance);
        }

        private static void HandleTouchdownSpecialTeams(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            AssertParametersLength(values, 2);

            string player = values[0];
            int distance = int.Parse(values[1]);

            var d = GetOrCreateGameDefense(scoringTeam, week);
            AddSpecialTeamsTouchdownYards(d, distance);
        }

        private static void HandleSafety(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            var d = GetOrCreateGameDefense(scoringTeam, week);
            d.SAFETY++;
        }

        private static void HandleEndzoneRecovery(FanastySeason season, int week, NFLPlayer scoringTeam, string[] values)
        {
            var d = GetOrCreateGameDefense(scoringTeam, week);
            AddSpecialTeamsTouchdownYards(d, 1);
        }

        private static NFLPlayer FindPlayerByName(FanastySeason season, string name)
        {
            return season.GetPlayerByIndex(name);
        }

        private static void AddTouchdownYards(Passing passing, int yards)
        {
            if (passing.TD_YDS == null)
            {
                passing.TD_YDS = new List<int>();
            }

            passing.TD_YDS.Add(yards);
        }

        private static void AddTouchdownYards(Rushing rushing, int yards)
        {
            if (rushing.TD_YDS == null)
            {
                rushing.TD_YDS = new List<int>();
            }

            rushing.TD_YDS.Add(yards);
        }

        private static void AddTouchdownYards(Receiving receiving, int yards)
        {
            if (receiving.TD_YDS == null)
            {
                receiving.TD_YDS = new List<int>();
            }

            receiving.TD_YDS.Add(yards);
        }

        private static void AddFieldGoalYards(Kicking kicking, int yards)
        {
            if (kicking.FG_YDS == null)
            {
                kicking.FG_YDS = new List<int>();
            }

            kicking.FG_YDS.Add(yards);
        }

        private static void AddTouchdownYards(Defense defense, int yards)
        {
            if (defense.TD_YDS == null)
            {
                defense.TD_YDS = new List<int>();
            }

            defense.TD_YDS.Add(yards);
        }

        private static void AddSpecialTeamsTouchdownYards(Defense defense, int yards)
        {
            if (defense.TD_ST_YDS == null)
            {
                defense.TD_ST_YDS = new List<int>();
            }

            defense.TD_ST_YDS.Add(yards);
        }

        private static Passing GetOrCreateGamePassing(NFLPlayer player, int week)
        {
            var game = GetOrCreateGame(player, week);
            if (game.Passing == null)
            {
                game.Passing = new Passing();
            }

            return game.Passing;
        }

        private static Rushing GetOrCreateGameRushing(NFLPlayer player, int week)
        {
            var game = GetOrCreateGame(player, week);
            if (game.Rushing == null)
            {
                game.Rushing = new Rushing();
            }

            return game.Rushing;
        }

        private static Receiving GetOrCreateGameReceiving(NFLPlayer player, int week)
        {
            var game = GetOrCreateGame(player, week);
            if (game.Receiving == null)
            {
                game.Receiving = new Receiving();
            }

            return game.Receiving;
        }

        private static Kicking GetOrCreateGameKicking(NFLPlayer player, int week)
        {
            var game = GetOrCreateGame(player, week);
            if (game.Kicking == null)
            {
                game.Kicking = new Kicking();
            }

            return game.Kicking;
        }

        private static Defense GetOrCreateGameDefense(NFLPlayer player, int week)
        {
            var game = GetOrCreateGame(player, week);

            if (game.Defense == null)
            {
                game.Defense = new Defense();
            }

            return game.Defense;
        }

        private static Game GetOrCreateGame(NFLPlayer player, int week)
        {
            var game = player.GameLog.SingleOrDefault(g => g.Week == week);
            if (game == null)
            {
                game = new Game();
                game.Week = week;
                player.GameLog.Add(game);
            }

            return game;
        }

        private static void AssertParametersLength(string[] values, int expectedLength)
        {
            if (values == null)
            {
                throw new ArgumentNullException("Handler was provided null parameters");
            }

            if (values.Length != expectedLength)
            {
                throw new ArgumentException($"Handler was provided {values.Length} parameters.  expected: {expectedLength}");
            }
        }
    }
}
