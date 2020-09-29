
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WaFFL.Evaluation
{
    public class ProFootballReferenceParser
    {
        /// <summary />
        private readonly string SeasonScheduleUri = "https://www.pro-football-reference.com/years/{0}/games.htm";

        /// <summary />
        private readonly string[] Delimiters = new string[] { "\n" };

        /// <summary />
        private WebClient httpClient;

        /// <summary />
        private FanastySeason context;

        /// <summary />
        private Action<string> callback;

        /// <summary />
        public ProFootballReferenceParser(Action<string> callback)
        {
            this.callback = callback;
            this.httpClient = new WebClient();
        }

        /// <summary />
        public void ParseSeason(int year, ref FanastySeason season)
        {
            if (this.context != null)
            {
                throw new InvalidOperationException("parsing action in progress");
            }

            if (season == null)
            {
                season = new FanastySeason();
                season.Year = year;
            }

            this.context = season;

            try
            {
                this.context.ClearAllPlayerGameLogs();
                string uri = string.Format(SeasonScheduleUri, year);
                ParseGames(uri);
            }
            finally
            {
                this.context.LastUpdated = DateTime.Now;
                season = this.context;

                this.context = null;
            }
        }

        public void UpdatePlayerInjuryStatus(int year, ref FanastySeason season, Func<NFLPlayer, bool> update)
        {
            string uri = string.Format(SeasonScheduleUri, year);

            foreach (var player in season.GetAllPlayers())
            {
                if (update(player))
                {
                    UpdatePlayerMetadata(player, player.PlayerPageUri, uri);
                }
            }
        }

        private void ParseGames(string uri)
        {
            string xhtml = this.httpClient.DownloadString(uri);
            var games = this.ExtractRawGames(xhtml);
            foreach (var game in games)
            {
                bool continueParsing = this.ParseGame(game, uri);
                if (!continueParsing)
                {
                    break;
                }
            }
        }

        private bool ParseGame(XElement game, string baseUri)
        {
            var fields = game.Elements().ToList();
            string week = fields[0].Value;
            string winner = fields[4].Value;
            string location = fields[5].Value;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = "vs";
            }
            string loser = fields[6].Value;

            this.callback(string.Format("Week {0} - {1} {2} {3}", week, winner, location, loser));

            var boxscore = fields[7];
            bool completed = boxscore.Value.Equals("boxscore");
            if (completed)
            {
                string uri = boxscore.Element("a").Attribute("href").Value;
                string boxscoreUri = new Uri(new Uri(baseUri), uri).AbsoluteUri;
                string xhtml = this.httpClient.DownloadString(boxscoreUri);
                
                var players = ExtractPlayerOffense(xhtml);
                foreach (var player in players)
                {
                    if (player.Elements().ToList().First().Attribute("data-append-csv") == null)
                    {
                        // boxscore contains player stat line with no player id - we are missing stats
                        Console.WriteLine("Invalid player in boxscore: " + boxscoreUri);
                        continue;
                    }

                    RecordPlayerStats(baseUri, week, player);
                }

                var kickers = ExtractPlayerKicking(xhtml);
                foreach (var kicker in kickers)
                {
                    RecordKickingStats(baseUri, week, kicker);
                }

                var defensivePlayers = ExtractDefense(xhtml);
                RecordDefensiveStats(week, defensivePlayers);

                var scores = ExtractScoringPlays(xhtml);
                foreach (var score in scores)
                {
                    RecordScoringPlays(week, score);
                }

            }

            return completed;
        }

        private void RecordPlayerStats(string baseUri, string week, XElement element)
        {
            NFLPlayer player = ExtractPlayerInfo(element, baseUri);
            AssignTeam(player, element);

            var values = element.Elements().ToList();
            Game game = new Game();
            game.Week = int.Parse(week);
            //game2.Opponent = this.context.GetTeam(values[1].Value);

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

        private void RecordKickingStats(string baseUri, string week, XElement element)
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

            NFLPlayer player = ExtractPlayerInfo(element, baseUri);
            AssignTeam(player, element);

            Game game = new Game();
            game.Week = int.Parse(week);
            //game2.Opponent = this.context.GetTeam(values[1].Value);

            Kicking k = game.Kicking = new Kicking();

            k.FGM = string.IsNullOrWhiteSpace(rawFGM) ? 0 : int.Parse(rawFGM);
            k.FGA = string.IsNullOrWhiteSpace(rawFGA) ? 0 : int.Parse(rawFGA);

            k.XPM = string.IsNullOrWhiteSpace(rawXPM) ? 0 : int.Parse(rawXPM);
            k.XPA = string.IsNullOrWhiteSpace(rawXPA) ? 0 : int.Parse(rawXPA);

            player.GameLog.Add(game);
        }

        private void RecordDefensiveStats(string week, List<XElement> element)
        {
            var teams = element.GroupBy(e => e.Elements().ToList()[1].Value);
            foreach (var team in teams)
            {
                var teamCode = team.Key;

                // extract player
                NFLPlayer player = this.context.GetPlayer(teamCode, p =>
                {
                    p.PlayerPageUri = string.Format("/teams/{0}/{1}.htm", teamCode.ToLowerInvariant(), this.context.Year);
                    p.Team = this.context.GetTeam(teamCode);
                    p.Name = DataConverter.ConvertToName(teamCode);
                    p.Position = FanastyPosition.DST;
                });

                double sacks = 0.0;
                int interceptions = 0;
                int interceptionYards = 0;
                int interceptionTouchdowns = 0;
                int fumblesRecovered = 0;
                int fumbleYards = 0;
                int fumbleTouchdowns = 0;

                foreach (var te in team)
                {
                    var values = te.Elements().ToList();

                    sacks += Convert.ToDouble(values[7].Value);
                    interceptions += Convert.ToInt32(values[2].Value);
                    interceptionYards += Convert.ToInt32(values[3].Value);
                    interceptionTouchdowns += Convert.ToInt32(values[4].Value);
                    fumblesRecovered += Convert.ToInt32(values[13].Value);
                    fumbleYards += Convert.ToInt32(values[14].Value);
                    fumbleTouchdowns += Convert.ToInt32(values[15].Value);
                }

                Game game = new Game();
                game.Week = int.Parse(week);
                //game2.Opponent = this.context.GetTeam(values[1].Value);

                Defense dst = game.Defense = new Defense();
                dst.SACK = Convert.ToInt32(sacks);
                dst.INT = interceptions;
                dst.YDS_INT = interceptionYards;
                dst.TD_INT = interceptionTouchdowns;
                dst.FUM = fumblesRecovered;
                dst.YDS_FUM = fumbleYards;
                dst.TD_FUM = fumbleTouchdowns;

                player.GameLog.Add(game);
            }
        }

        private void RecordScoringPlays(string week, XElement score)
        {
            string text = score.Elements().ToList()[3].Value;
            var parser = new ScoringPlayParser(this.context, int.Parse(week));
            parser.Parse(score);
        }

        private NFLPlayer ExtractPlayerInfo(XElement element, string baseUri)
        {
            var fields = element.Elements().ToList();
            string playerid = fields[0].Attribute("data-append-csv").Value;
            string name = fields[0].Value;

            // the player must have an id and name, but position is optional
            if (playerid == null || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException();
            }

            NFLPlayer player = this.context.GetPlayer(playerid, p =>
            {
                p.PlayerPageUri = fields[0].Element("a")?.Attribute("href")?.Value;
                p.Name = name;
                UpdatePlayerMetadata(p, element, baseUri);
            });

            return player;
        }

        private void UpdatePlayerMetadata(NFLPlayer player, XElement element, string baseUri)
        {
            var fields = element.Elements().ToList();
            string uri = fields[0].Element("a").Attribute("href").Value;
            UpdatePlayerMetadata(player, uri, baseUri);
        }

        private void UpdatePlayerMetadata(NFLPlayer player, string playerPageUri, string baseUri)
        {
            string playerUri = new Uri(new Uri(baseUri), playerPageUri).AbsoluteUri;
            string xhtml = this.httpClient.DownloadString(playerUri);

            const string start = "            <div class=\"section_wrapper\" id=\"injury\">";
            const string end = "</div>";
            string[] exclude = { };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude, true);
            if (parsedElement != null)
            {
                var status = parsedElement.Element("h3").Value;
                var reason = parsedElement.Element("p").Value;
                Console.WriteLine("INJURY ::: {0} {1}", status, reason);

                player.Status = new InjuryStatus() { Reason = reason };
                if (status == "Injured Reserve")
                {
                    player.Status.Status = PlayerInjuryStatus.InjuredReserve;
                }
                else if (status == "Out")
                {
                    player.Status.Status = PlayerInjuryStatus.Out;
                }
                else if (status == "Doubtful")
                {
                    player.Status.Status = PlayerInjuryStatus.Doubtful;
                }
                else if (status == "Questionable")
                {
                    player.Status.Status = PlayerInjuryStatus.Questionable;
                }
                else if (status == "Probable")
                {
                    player.Status.Status = PlayerInjuryStatus.Probable;
                }
                else
                {
                    throw new InvalidOperationException("Unknown injury status");
                }

            }
            else
            {
                player.Status = null;
            }


            // TODO parse this properly
            if (xhtml.Contains("<strong>Position</strong>: QB"))
            {
                player.Position = FanastyPosition.QB;
            }
            else if (xhtml.Contains("<strong>Position</strong>: RB") || xhtml.Contains("<strong>Position</strong>: FB"))
            {
                player.Position = FanastyPosition.RB;
            }
            else if (xhtml.Contains("<strong>Position</strong>: WR") || xhtml.Contains("<strong>Position</strong>: TE"))
            {
                player.Position = FanastyPosition.WR;
            }
            else if (xhtml.Contains("<strong>Position</strong>: K"))
            {
                player.Position = FanastyPosition.K;
            }
            else
            {
                player.Position = FanastyPosition.UNKNOWN;
            }
        }

        private void AssignTeam(NFLPlayer player, XElement element)
        {
            // if the player has played for multiple teams, we 
            // only want the most recent team that they have 
            // played for.
            var fields = element.Elements().ToList();
            string teamcode = fields[1].Value;

            NFLTeam team = this.context.GetTeam(teamcode);
            if (player.Team != team)
            {
                // if team has changed, we will update -- we only need to track the most recent
                // team.  This is all that matters.
                player.Team = team;
            }
        }

        private List<XElement> ExtractDefense(string xhtml)
        {
            const string start = "  <table class=\"sortable stats_table\" id=\"player_defense\" data-cols-to-freeze=\"1\"><caption>Defense Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Element("tbody")
                                       .Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractPlayerKicking(string xhtml)
        {
            const string start = "  <table class=\"sortable stats_table\" id=\"kicking\" data-cols-to-freeze=\"1\"><caption>Kicking & Punting Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Element("tbody")
                                       .Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractPlayerOffense(string xhtml)
        {
            const string start = "  <table class=\"sortable stats_table\" id=\"player_offense\" data-cols-to-freeze=\"1\"><caption>Passing, Rushing, & Receiving Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Element("tbody")
                                       .Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
        }

        private List<XElement> ExtractScoringPlays(string xhtml)
        {
            const string start = "  <table class=\"stats_table\" id=\"scoring\" data-cols-to-freeze=\"2\"><caption>Scoring Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var scores = parsedElement.Element("tbody")
                                      .Elements("tr")
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
            const string start = "  <table class=\"sortable stats_table\" id=\"games\" data-cols-to-freeze=\"1\"><caption>Week-by-Week Games Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var games = parsedElement.Element("tbody")
                                     .Elements("tr")
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

        private XElement ExtractRawData(string xhtml, string startingline, string endingline, string[] exclude, bool optional = false)
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
                    raw = raw.Replace("data-cols-to-freeze=1", "data-cols-to-freeze=\"1\"");
                    raw = raw.Replace("data-cols-to-freeze=2", "data-cols-to-freeze=\"2\"");
                    raw = raw.Replace("& ", "&amp; ");
                    raw = raw.Replace("<br>", " ");
                    raw = raw.Replace("<br />", " ");
                    raw = raw.Replace("data-tip=\"<b>Yards per Punt</b>", "data-tip=\"Yards per Punt");
                    raw = raw.Replace("</td></td>", "</td>");

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

    public class ScoringPlayParser
    {
        // scoring types that should have an extra point (not all do)
        private static readonly Dictionary<Regex, Action> tdscores = new Dictionary<Regex, Action>()
        {
            // td passes
            { new Regex(@"([\w ']*) (\d*) yard pass from ([\w ']*)", RegexOptions.Compiled), null },

            // td rushing
            { new Regex(@"([\w ']*) (\d*) yard rush", RegexOptions.Compiled), null },

            // td defense
            { new Regex(@"([\w ']*) fumble recovery in end zone", RegexOptions.Compiled), null },
            { new Regex(@"([\w ']*) (\d*) yard fumble return", RegexOptions.Compiled), null },
            { new Regex(@"([\w ']*) (\d*) yard interception return", RegexOptions.Compiled), null },
            { new Regex(@"([\w ']*) (\d*) yard blocked punt return", RegexOptions.Compiled), null },
            { new Regex(@"([\w ']*) (\d*) yard kickoff return", RegexOptions.Compiled), null },
            { new Regex(@"([\w ']*) (\d*) yard punt return", RegexOptions.Compiled), null },
        };

        // extra point types
        private static readonly Dictionary<Regex, Action> xp = new Dictionary<Regex, Action>()
        {
            // extra points
            { new Regex(@"\(([\w ']*) kick\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\w ']*) kick failed\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\w ']*) run\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(run failed\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(([\w ']*) pass from ([\w ']*)\)$", RegexOptions.Compiled), null },
            { new Regex(@"\(pass failed\)$", RegexOptions.Compiled), null },
        };

        // scoring types that do not have an extra point
        private static readonly Dictionary<Regex, Action> otherscores = new Dictionary<Regex, Action>()
        {
            // field goals
            { new Regex(@"([\w ']*) (\d*) yard field goal", RegexOptions.Compiled), null },

            // safety
            { new Regex(@"Safety, ([\w ']*) tackled in end zone by ([\w ']*)", RegexOptions.Compiled), null },
            { new Regex(@"Safety, ([\w ']*) sacked in end zone by ([\w ']*)", RegexOptions.Compiled), null },
        };

        private readonly FanastySeason _season;
        private readonly int _week;

        public ScoringPlayParser(FanastySeason season, int week)
        {
            _season = season;
            _week = week;
        }

        public bool Parse(XElement score)
        {
            string text = score.Elements().ToList()[3].Value;

            bool success = TryProcess(tdscores, text, () =>
            {
                bool xpsuccess = TryProcess(xp, text);
                if (!xpsuccess)
                {
                    // happens when a TD wins the game
                    Console.WriteLine("NO-XP FOUND: {0}", text);
                }
            });

            if (!success)
            {
                return true;
            }

            success = TryProcess(otherscores, text);

            if (!success)
            {
                return true;
            }

            Console.WriteLine("UNKNOWN-SCORE: {0}", text);
            return false;
        }

        private static bool TryProcess(Dictionary<Regex, Action> actions, string text, Action followup = null)
        {
            foreach (var set in actions)
            {
                Regex re = set.Key;
                if (re.IsMatch(text))
                {
                    set.Value?.Invoke();
                    followup?.Invoke();
                    return true;
                }
            }

            return false;
        }
    }
}
