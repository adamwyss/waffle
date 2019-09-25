
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
                throw new InvalidOperationException();
            }

            this.context = new FanastySeason();
            this.context.Year = year;

            string uri = string.Format(SeasonScheduleUri, year);
            ParseGames(uri);

            this.context.LastUpdated = DateTime.Now;
            season = this.context;

            this.context = null;
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

                // <table class="stats_table" id="scoring" data-cols-to-freeze=2><caption>Scoring Table</caption>

                var players = ExtractPlayerOffense(xhtml);
                foreach (var player in players)
                {
                    RecordPlayerStats(baseUri, week, player);
                }

                var kickers = ExtractPlayerKicking(xhtml);
                foreach (var kicker in kickers)
                {
                    RecordKickingStats(baseUri, week, kicker);
                }

                // <table class="sortable stats_table" id="pbp" data-cols-to-freeze=2><caption>Full Play-By-Play Table</caption>
            }

            System.Threading.Thread.Sleep(200);

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

            // In 2016, the average kick was from 37.7 yards away; the average
            // successful kick was from 36.2 yards out - while the average miss was from 46.2 yards away.

            k.FGM_01to19 = 0;
            k.FGA_01to19 = 0;

            k.FGM_20to29 = 0;
            k.FGA_20to29 = 0;

            k.FGM_30to39 = string.IsNullOrWhiteSpace(rawFGM) ? 0 : int.Parse(rawFGM);
            k.FGA_30to39 = string.IsNullOrWhiteSpace(rawFGA) ? 0 : int.Parse(rawFGA);

            k.FGM_40to49 = 0;
            k.FGA_40to49 = 0;

            k.FGM_50plus = 0;
            k.FGA_50plus = 0;

            k.XPM = string.IsNullOrWhiteSpace(rawXPM) ? 0 : int.Parse(rawXPM);
            k.XPA = string.IsNullOrWhiteSpace(rawXPA) ? 0 : int.Parse(rawXPA);

            player.GameLog.Add(game);
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

            NFLPlayer player = this.context.GetPlayer(playerid);

            // capture the player page url
            player.PlayerPageUri = fields[0].Element("a")?.Attribute("href")?.Value;

            if (string.IsNullOrEmpty(player.Name))
            {
                player.Name = name;
            }
            else if (player.Name != name)
            {
                throw new InvalidOperationException("player name changed");
            }

            if (player.Position == FanastyPosition.UNKNOWN)
            {
                player.Position = GetPosition(element, baseUri);
            }

            return player;
        }

        private FanastyPosition GetPosition(XElement element, string baseUri)
        {
            var fields = element.Elements().ToList();
            string uri = fields[0].Element("a").Attribute("href").Value;
            string boxscoreUri = new Uri(new Uri(baseUri), uri).AbsoluteUri;
            string xhtml = this.httpClient.DownloadString(boxscoreUri);

            // TODO parse this properly
            if (xhtml.Contains("<strong>Position</strong>: QB"))
            {
                return FanastyPosition.QB;
            }
            else if (xhtml.Contains("<strong>Position</strong>: RB") || xhtml.Contains("<strong>Position</strong>: FB"))
            {
                return FanastyPosition.RB;
            }
            else if (xhtml.Contains("<strong>Position</strong>: WR") || xhtml.Contains("<strong>Position</strong>: TE"))
            {
                return FanastyPosition.WR;
            }
            else if (xhtml.Contains("<strong>Position</strong>: K"))
            {
                return FanastyPosition.K;
            }

            return FanastyPosition.UNKNOWN;
        }

        private void AssignTeam(NFLPlayer player, XElement element)
        {
            // if the player has played for multiple teams, we 
            // only want the most recent team that they have 
            // played for.
            var fields = element.Elements().ToList();
            string teamcode = fields[1].Value;

            NFLTeam team = this.context.GetTeam(teamcode);

            if (player.Team == null)
            {
                player.Team = team;
            }
            else if (player.Team == team)
            {
                // do nothing
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private List<XElement> ExtractPlayerKicking(string xhtml)
        {
            const string start = "  <table class=\"sortable stats_table\" id=\"kicking\" data-cols-to-freeze=1><caption>Kicking & Punting Table</caption>";
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
            const string start = "  <table class=\"sortable stats_table\" id=\"player_offense\" data-cols-to-freeze=1><caption>Passing, Rushing, & Receiving Table</caption>";
            const string end = "</tbody></table>";
            string[] exclude = { "   <colgroup><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col><col></colgroup>" };
            XElement parsedElement = ExtractRawData(xhtml, start, end, exclude);

            var players = parsedElement.Element("tbody")
                                       .Elements("tr")
                                       .Where(IsPlayerRow)
                                       .ToList();
            return players;
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
            const string start = "  <table class=\"sortable stats_table\" id=\"games\" data-cols-to-freeze=1><caption>Week-by-Week Games Table</caption>";
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

        private XElement ExtractRawData(string xhtml, string startingline, string endingline, string[] exclude)
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
                    raw = raw.Replace("& ", "&amp; ");
                    raw = raw.Replace("<br>", " ");
                    raw = raw.Replace("<br />", " ");
                    raw = raw.Replace("data-tip=\"<b>Yards per Punt</b>", "data-tip=\"Yards per Punt");

                    // parse and return
                    return XElement.Parse(raw);
                }
            }

            throw new InvalidOperationException("The specified data was not found");
        }
    }
}
