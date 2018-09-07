using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class ESPNEntityParser
    {
        /// <summary />
        private const string PlayerPassingUri = @"http://espn.go.com/nfl/statistics/player/_/stat/passing/{0}";

        /// <summary />
        private const string PlayerRushingUri = @"http://espn.go.com/nfl/statistics/player/_/stat/rushing/{0}";

        /// <summary />
        private const string PlayerReceivingUri = @"http://espn.go.com/nfl/statistics/player/_/stat/receiving/{0}";

        /// <summary />
        private const string PlayerKickingUri = @"http://espn.go.com/nfl/statistics/player/_/stat/kicking/year/{0}";

        /// <summary />
        private const string TeamKickingUri = @"http://espn.go.com/nfl/statistics/team/_/stat/kicking/year/{0}";

        /// <summary />
        private const string TeamDefenseUri = @"http://espn.go.com/nfl/statistics/team/_/stat/defense/year/{0}";

        /// <summary />
        private const string GameLogUri = @"http://sports.espn.go.com/nfl/players/gamelog?playerId={1}&sYear={0}";

        /// <summary />
        private readonly string[] Delimiters = new string[] { "\n" };

        /// <summary />
        private WebClient httpClient;

        /// <summary />
        private FanastySeason context;

        private Action<string> callback;

        /// <summary />
        public ESPNEntityParser(Action<string> callback)
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
                        
            string uri = string.Format(PlayerPassingUri, year);
            this.ParseOffensiveLeaders(uri, this.ParsePassingLeaderItem, null);

            uri = string.Format(PlayerRushingUri, year);
            this.ParseOffensiveLeaders(uri, this.ParseRushingLeaderItem, null);

            uri = string.Format(PlayerReceivingUri, year);
            this.ParseOffensiveLeaders(uri, this.ParseReceivingLeaderItem, null);

            uri = string.Format(PlayerKickingUri, year);
            this.ParseOffensiveLeaders(uri, this.ParseKickingLeaderItem, null);

            uri = string.Format(TeamDefenseUri, year);
            this.ParseDefensiveLeaders(uri, this.ParseDefenseLeaderItem, null);

            this.CalculateStats();

            this.context.LastUpdated = DateTime.Now;
            season = this.context;

            this.context = null;
        }


        private struct Reference<T>
        {
            public int Average { get; set; }
            public T Context { get; set; }
        }

        private void CalculateStats()
        {
            this.ThrowIfNotNull(this.context.ReplacementValue);

            int maxGames = 0;

            {
                NFLPlayer[] players = this.context.GetAllPlayers().ToArray();

                int count = players.Length;
                int middle = count / 2;

                Reference<NFLPlayer>[] tally = new Reference<NFLPlayer>[count];

                for (int i = 0; i < count; i++)
                {
                    int p = players[i].FanastyPoints();
                    int g = players[i].GamesPlayed();

                    tally[i] = new Reference<NFLPlayer>();
                    if (g > 0)
                    {
                        tally[i].Average = p / g;


                        // hack to get games played for defense stats
                        maxGames = Math.Max(g, maxGames);
                    }

                    tally[i].Context = players[i];
                }

                var qb = from p in tally
                         where p.Context.Position == FanastyPosition.QB
                         orderby p.Average descending
                         select p;

                var rb = from p in tally
                         where p.Context.Position == FanastyPosition.RB
                         orderby p.Average descending
                         select p;

                var wr = from p in tally
                         where p.Context.Position == FanastyPosition.WR
                         orderby p.Average descending
                         select p;

                var k = from p in tally
                        where p.Context.Position == FanastyPosition.K
                        orderby p.Average descending
                        select p;

                PositionBaseline baseline = this.context.ReplacementValue = new PositionBaseline();

                // to calculate the replacement point value, we stack rank the players, then
                // pick the player at the postion that a replacment could be easily gotten off of wavers.

                // example:  16 QB roster spots + 16 Flex positions, but assume only 6 of them are filled with QB's
                // example:  3 RB roster spots * 16 teams, assume very few (0) flex positions will contain a RB.

                int qbReplacePos = Math.Min(22, qb.Count() - 1);
                baseline.QB = qb.ElementAt(qbReplacePos).Average;

                int rbReplacePos = Math.Min(48, rb.Count() - 1);
                baseline.RB = rb.ElementAt(rbReplacePos).Average;

                int wrReplacePos = Math.Min(48, wr.Count() - 1);
                baseline.WR = wr.ElementAt(wrReplacePos).Average;

                int kReplacePos = Math.Min(21, k.Count() - 1);
                baseline.K = k.ElementAt(kReplacePos).Average;

            }

            {
                NFLTeam[] teams = this.context.GetAllTeams().ToArray();

                int count = teams.Length;

                Reference<NFLTeam>[] tally = new Reference<NFLTeam>[count];

                for (int i = 0; i < count; i++)
                {
                    int p = teams[i].ESPNTeamDefense.Estimate_Points();

                    tally[i] = new Reference<NFLTeam>();

                    if (maxGames != 0)
                        tally[i].Average = p / maxGames;
                    else
                        tally[i].Average = 0;

                    tally[i].Context = teams[i];
                }

                Array.Sort(tally, (x, y) => y.Average - x.Average);

                PositionBaseline baseline = this.context.ReplacementValue;

                int dstReplacePos = Math.Min(21, tally.Count() - 1);
                baseline.DST = tally[dstReplacePos].Average;
            }
        }

        /// <summary />
        private void ParseOffensiveLeaders(string uri, Action<XElement[], object> callback, object state)
        {
            do
            {
                if (uri.StartsWith("//"))
                {
                    uri = "http:" + uri;
                }

                string xhtml = this.httpClient.DownloadString(uri);

                IEnumerable<XElement> dataRows = this.ExtractRawPlayerStats(xhtml, out uri);
                foreach (XElement row in dataRows)
                {
                    XElement[] values = row.Elements("td").ToArray();

                    try
                    {
                        callback(values, state);
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Multiple players detected.");
                        // we are parsing the same player twice.
                    }
                }
            }
            while (uri != null);
        }

        /// <summary />
        private void ParseDefensiveLeaders(string uri, Action<XElement[], object> callback, object state)
        {
            string xhtml = this.httpClient.DownloadString(uri);

            IEnumerable<XElement> dataRows = this.ExtractRawTeamStats(xhtml);
            foreach (XElement row in dataRows)
            {
                XElement[] values = row.Elements("td").ToArray();

                try
                {
                    callback(values, state);
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Multiple teams detected.");
                    // we are parsing the same team twice.
                }
            }
        }

        /// <summary />
        private void ParsePassingLeaderItem(XElement[] values, object state)
        {
            NFLPlayer player = this.ExtractPlayerInfo(values[1]);
            this.AssignTeam(player, values[2].Value);

            this.ParseGameLog(player);
        }

        /// <summary />
        private void ParseRushingLeaderItem(XElement[] values, object state)
        {
            NFLPlayer player = this.ExtractPlayerInfo(values[1]);
            this.AssignTeam(player, values[2].Value);

            this.ParseGameLog(player);
        }

        /// <summary />
        private void ParseReceivingLeaderItem(XElement[] values, object state)
        {
            NFLPlayer player = this.ExtractPlayerInfo(values[1]);
            this.AssignTeam(player, values[2].Value);

            this.ParseGameLog(player);
        }

        /// <summary />
        private void ParseKickingLeaderItem(XElement[] values, object state)
        {
            NFLPlayer player = this.ExtractPlayerInfo(values[1]);
            this.AssignTeam(player, values[2].Value);

            this.ParseGameLog(player);
        }

        /// <summary />
        private void ParseDefenseLeaderItem(XElement[] values, object state)
        {
            NFLTeam team = this.ExtractTeamInfo(values[1]);
            NFLPlayer player = this.ExtractDefensivePlayerInfo(team);

            this.ThrowIfNotNull(team.ESPNTeamDefense);
            ESPNDefenseLeaders td = team.ESPNTeamDefense = new ESPNDefenseLeaders();

            td.SOLO = int.Parse(values[2].Value);
            td.AST = int.Parse(values[3].Value);

            td.SACK = (int)double.Parse(values[5].Value);
            td.YDSL = int.Parse(values[6].Value);

            td.PD = int.Parse(values[7].Value);
            td.INT = int.Parse(values[8].Value);
            td.YDS = int.Parse(values[9].Value);
            td.LONG = int.Parse(values[10].Value);
            td.TD_INT = int.Parse(values[11].Value);

            td.FF = int.Parse(values[12].Value);
            td.REC = int.Parse(values[13].Value);
            td.TD_FUM = int.Parse(values[14].Value);
        }

        /// <summary />
        private IEnumerable<XElement> ExtractRawPlayerStats(string xhtml, out string pageUri)
        {
            XElement parsedValue = null;
            pageUri = null;
         
            // extract the html data table that we are interested in.
            string[] lines = xhtml.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                // get the stats
                if (lines[i] == "<div class=\"span-6\">" &&
                    lines[i + 1] == "\t<div id=\"my-players-table\" class=\"col-main\">" &&
                    lines[i + 5] == "\t\t\t\t<table class=\"tablehead\" cellpadding=\"3\" cellspacing=\"1\">")
                {
                    string data = lines[i + 5].TrimStart('\t') + lines[i + 6].TrimStart('\t') + lines[i + 7].TrimStart('\t');
                    data = data.Replace("&nbsp;", string.Empty);
                    data = data.Replace("</a></td></tr></tr><tr class=", "</a></td></tr><tr class=");
                    parsedValue = XElement.Parse(data);

                    // get past the stuff we just parsed
                    i = i + 8;
                }

                // get the page link
                if (parsedValue != null &&
                    lines[i] == "\t\t\t<div class=\"mod-footer\">" &&
                    lines[i + 3] == "<div class=\"controls\" style=\"float: right;\">")
                {
                    string data = lines[i + 3] + lines[i + 4] + lines[i + 5] + lines[i + 6] + lines[i + 7];
                    if (lines[i + 7] == "</a>")
                    {
                        data += lines[i + 8];
                    }

                    XElement parsed = XElement.Parse(data);
                    XElement anchor = parsed.Elements().Last();
                    if (anchor.Name == "a")
                    {
                        pageUri = anchor.Attribute("href").Value;
                    }

                    // we got everything
                    break;
                }
            }

            // find all table rows that are not header rows (data rows only)
            var dataRows = from r in parsedValue.Elements("tr")
                           where r.Attribute("class") != null &&
                                 r.Attribute("class").Value != "colhead"
                           select r;

            return dataRows;
        }

        /// <summary />
        private IEnumerable<XElement> ExtractRawTeamStats(string xhtml)
        {
            XElement parsedValue = null;

            // extract the html data table that we are interested in.
            string[] lines = xhtml.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "<div class=\"span-6\">" &&
                    lines[i + 1] == "\t<div id=\"my-teams-table\" class=\"col-main\">" &&
                    lines[i + 5] == "\t\t\t\t<table class=\"tablehead\" cellpadding=\"3\" cellspacing=\"1\">")
                {
                    string data = lines[i + 5].TrimStart('\t') + lines[i + 6].TrimStart('\t') + lines[i + 7].TrimStart('\t');
                    data = data.Replace("&nbsp;", string.Empty);
                    parsedValue = XElement.Parse(data);
                    break;
                }
            }

            // find all table rows that are not header rows (data rows only)
            var dataRows = from r in parsedValue.Elements("tr")
                           where r.Attribute("class") != null &&
                                 r.Attribute("class").Value != "colhead"
                           select r;

            return dataRows;
        }

        /// <summary />
        private NFLPlayer ExtractPlayerInfo(XElement element)
        {
            int playerid = -1;
            string name = null;
            FanastyPosition position = FanastyPosition.UNKNOWN;

            XElement item = element.Elements("a").FirstOrDefault();
            if (item != null)
            {
                // assigns the player name
                name = item.Value;

                // assign the player id
                // current format-- http://espn.go.com/nfl/player/_/id/5636/will-allen

                XAttribute attrib = item.Attribute("href");
                string value = attrib.Value;
                int pos = value.LastIndexOf("id/");
                string subcanidate = value.Substring(pos + 3);
                pos = subcanidate.LastIndexOf("/");
                string canidate = subcanidate.Substring(0, pos);
                playerid = int.Parse(canidate);
            }

            // assign the position
            string value2 = element.Value;
            int pos2 = value2.LastIndexOf(",");
            string canidate2 = value2.Substring(pos2 + 1).Trim();
            switch (canidate2)
            {
                case "QB":
                    position = FanastyPosition.QB;
                    break;

                case "RB":
                case "FB":
                    position = FanastyPosition.RB;
                    break;

                case "WR":
                case "TE":
                    position = FanastyPosition.WR;
                    break;

                case "PK":
                    position = FanastyPosition.K;
                    break;
            }

            // the player must have an id and name, but position is optional
            if (playerid == -1 || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException();
            }

            NFLPlayer player = this.context.GetPlayer(playerid);

            if (string.IsNullOrEmpty(player.Name))
            {
                player.Name = name;
            }
            else if (player.Name != name)
            {
                throw new InvalidOperationException();
            }

            if (player.Position == FanastyPosition.UNKNOWN)
            {
                player.Position = position;
            }
            else if (player.Position != position)
            {
                throw new InvalidOperationException();
            }

            return player;
        }

        /// <summary />
        private NFLPlayer ExtractDefensivePlayerInfo(NFLTeam team)
        {
            var id = team.TeamCode.GetHashCode() * -1;
            NFLPlayer player = this.context.GetPlayer(id);

            string name = DataConverter.ConvertToName(team.TeamCode);
            if (string.IsNullOrEmpty(player.Name))
            {
                player.Name = name;
            }
            else if (player.Name != name)
            {
                throw new InvalidOperationException();
            }

            FanastyPosition position = FanastyPosition.DST;
            if (player.Position == FanastyPosition.UNKNOWN)
            {
                player.Position = position;
            }
            else if (player.Position != position)
            {
                throw new InvalidOperationException();
            }

            if (player.Team == null)
            {
                player.Team = team;
            }

            return player;
        }

        /// <summary />
        private NFLTeam ExtractTeamInfo(XElement element)
        {
            string name = null;
            string teamcode = null;

            XElement item = element.Elements("a").FirstOrDefault();
            if (item != null)
            {
                // find the team name
                name = item.Value;
            }

            teamcode = DataConverter.ConvertToCode(name);

            // the player must have an id and name, but position is optional
            if (string.IsNullOrEmpty(teamcode))
            {
                throw new InvalidOperationException();
            }

            return this.context.GetTeam(teamcode);
        }

        /// <summary />
        private void AssignTeam(NFLPlayer player, string teamcode)
        {
            // if the player has played for multiple teams, we 
            // only want the most recent team that they have 
            // played for.
            int pos = teamcode.LastIndexOf("/");
            if (pos != -1)
            {
                teamcode = teamcode.Substring(pos + 1);
            }

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

        /// <summary />
        private void ThrowIfNotNull(object value)
        {
            if (value != null)
            {
                throw new InvalidOperationException();
            }
        }

        private void ParseGameLog(NFLPlayer player)
        {
            if (player.GameLog.Count > 0)
            {
                return;
            }

            this.callback(string.Format("{0}, {1} ({2})", player.Name, player.Position.ToString(), player.Team.TeamCode));

            string uri = string.Format(GameLogUri, this.context.Year, player.ESPN_Identifier);
            string xhtml = this.httpClient.DownloadString(uri);

            // extract the html data table that we are interested in.
            string[] lines = xhtml.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("REGULAR SEASON GAME LOG"))
                {
                    string tempStr = lines[i];

                    int start = tempStr.LastIndexOf("<table");
                    int end = tempStr.LastIndexOf("</table>");

                    string statLine = tempStr.Substring(start, (end + 8 - start));

                    statLine = statLine.Replace("\"title=\"", "\" title=\"");
                    statLine = Regex.Replace(statLine, "<a href=\"[^\"]*\"></li>", "</li>");

                    XElement parsedValue = XElement.Parse(statLine);

                    var dataRows = from r in parsedValue.Elements("tr")
                                   where r.Attribute("class") != null &&
                                         (r.Attribute("class").Value.StartsWith("evenrow") ||
                                         r.Attribute("class").Value.StartsWith("oddrow"))
                                   select r;

                    foreach (XElement row in dataRows)
                    {
                        XElement[] values = row.Elements("td").ToArray();

                        // skip games that have not been played, bye-weeks & trades
                        if (values.Length == 4 && values[3].Value == "Did Not Play or did not accumulate any stats.")
                        {
                            Game game = new Game();
                            game.Week = this.context.GetWeek(values[0].Value, this.context.Year);
                            game.Opponent = this.context.GetTeam(values[1].Value.TrimStart('@'));
                            player.GameLog.Add(game);
                        }
                        else if (values.Length > 2 && this.context.IsBadTeamCode(values[1].Value))
                        {
                            // some games, like all star games are listed in the player gamelog.
                            continue;
                        }
                        else if (values.Length == 18 && player.Position == FanastyPosition.QB)
                        {
                            Game game = new Game();
                            game.Week = this.context.GetWeek(values[0].Value, this.context.Year);
                            game.Opponent = this.context.GetTeam(values[1].Value);
                            Passing p = game.Passing = new Passing();
                            p.CMP = int.Parse(values[3].Value);
                            p.ATT = int.Parse(values[4].Value);
                            p.YDS = int.Parse(values[5].Value);
                            p.LONG = int.Parse(values[8].Value);
                            p.TD = int.Parse(values[9].Value);
                            p.INT = int.Parse(values[10].Value);
                            Rushing r = game.Rushing = new Rushing();
                            r.CAR = int.Parse(values[13].Value);
                            r.YDS = int.Parse(values[14].Value);
                            r.LONG = int.Parse(values[16].Value);
                            r.TD = int.Parse(values[17].Value);
                            player.GameLog.Add(game);
                        }
                        else if (values.Length == 15 && player.Position == FanastyPosition.RB)
                        {
                            Game game = new Game();
                            game.Week = this.context.GetWeek(values[0].Value, this.context.Year);
                            game.Opponent = this.context.GetTeam(values[1].Value);
                            Rushing r = game.Rushing = new Rushing();
                            r.CAR = int.Parse(values[3].Value);
                            r.YDS = int.Parse(values[4].Value);
                            r.LONG = int.Parse(values[6].Value);
                            r.TD = int.Parse(values[7].Value);
                            Receiving c = game.Receiving = new Receiving();
                            c.REC = int.Parse(values[8].Value);
                            c.YDS = int.Parse(values[9].Value);
                            c.LONG = int.Parse(values[11].Value);
                            c.TD = int.Parse(values[12].Value);
                            Fumbles f = game.Fumbles = new Fumbles();
                            f.FUM = int.Parse(values[13].Value);
                            f.LOST = int.Parse(values[14].Value);
                            player.GameLog.Add(game);
                        }
                        else if (values.Length == 16 && player.Position == FanastyPosition.WR)
                        {
                            Game game = new Game();
                            game.Week = this.context.GetWeek(values[0].Value, this.context.Year);
                            game.Opponent = this.context.GetTeam(values[1].Value);
                            Receiving c = game.Receiving = new Receiving();
                            c.REC = int.Parse(values[3].Value);
                            c.YDS = int.Parse(values[5].Value);
                            c.LONG = int.Parse(values[7].Value);
                            c.TD = int.Parse(values[8].Value);
                            Rushing r = game.Rushing = new Rushing();
                            r.CAR = int.Parse(values[9].Value);
                            r.YDS = int.Parse(values[10].Value);
                            r.LONG = int.Parse(values[12].Value);
                            r.TD = int.Parse(values[13].Value);

                            Fumbles f = game.Fumbles = new Fumbles();
                            f.FUM = int.Parse(values[14].Value);
                            f.LOST = int.Parse(values[15].Value);
                            player.GameLog.Add(game);
                        }
                        else if (values.Length == 15 && player.Position == FanastyPosition.K)
                        {
                            Game game = new Game();
                            game.Week = this.context.GetWeek(values[0].Value, this.context.Year);
                            game.Opponent = this.context.GetTeam(values[1].Value);

                            Kicking k = game.Kicking = new Kicking();
                            k.LONG = int.Parse(values[11].Value);
                            string[] temp = values[8].Value.Split(new string[] { "/" }, 2, StringSplitOptions.RemoveEmptyEntries);
                            int misses = int.Parse(temp[1]) - int.Parse(values[3].Value) - int.Parse(values[4].Value) - int.Parse(values[5].Value)
                                        - int.Parse(values[6].Value) - int.Parse(values[7].Value);

                            // ESPN no longer records misses for fieldgoals, so we will just estimate them at 35 yards

                            k.FGM_01to19 = int.Parse(values[3].Value);
                            k.FGA_01to19 = int.Parse(values[3].Value);

                            k.FGM_20to29 = int.Parse(values[4].Value);
                            k.FGA_20to29 = int.Parse(values[4].Value);

                            k.FGM_30to39 = int.Parse(values[5].Value);

                            // Don't know misses so will guess it is a 35 yarder
                            k.FGA_30to39 = int.Parse(values[5].Value) + misses;

                            k.FGM_40to49 = int.Parse(values[6].Value);
                            k.FGA_40to49 = int.Parse(values[6].Value);

                            k.FGM_50plus = int.Parse(values[7].Value);
                            k.FGA_50plus = int.Parse(values[7].Value);

                            k.XPM = int.Parse(values[12].Value);
                            k.XPA = int.Parse(values[13].Value);
                            player.GameLog.Add(game);
                        }
                    }

                    break;
                }
            }
        }
    }
}
