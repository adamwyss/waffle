using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class WaFFLRoster
    {
        private static readonly Dictionary<string, string> mappings = new Dictionary<string, string>()
        {
        //  { "Correct Spelling", "WaFFL Misspelling" },
            // 2019
            { "Patrick Mahomes", "Pat Mahomes" },
            { "Saquon Barkley", "Saquan Barclay" },
            { "Odell Beckham", "O'Dell Beckham Jr" },
            { "Le'Veon Bell", "LeVeon Bell" },
            { "Phillip Lindsay", "Philip Lindsay" },
            { "Devonta Freeman", "Davonta Freeman" },
            { "T.Y.Hilton", "TY Hilton" },
            { "Matthew Stafford", "Matt Stafford" },
            { "Deshaun Watson", "DeShaun Watson" },
            { "Julian Edelman", "Julien Edelman" },
            { "Alshon Jeffery", "Alshon Jeffrey" },
            { "Stephen Gostkowski", "Stephan Gostkowski" },
            { "San Francisco 49ers", "San Fransciso 49ers" },
            { "D.J. Moore", "DJ Moore " },
            { "DeVante Parker", "DaVante Parker" },
            //2020
            { "J.K. Dobbins", "JK Dobbins" },
            { "Adam Thielen", "Adam Theilen" },

        };

        /// <summary />
        private const string WaFFLRosterUri = @"http://www.thewaffl.net/Rosters.php";

        /// <summary />
        private static readonly string[] HtmlScrubExpressions = new string[]
            {
                // remove all html tags
                @"<[^>]+>",

                // remove all html code entities
                @"&[^;]+",

                // remove all white space characters
                @"\s+",
            };

        /// <summary />
        private static WaFFLRoster instance;

        /// <summary />
        private string rosterText;

        /// <summary />
        private WaFFLRoster()
        {
            WebClient client = new WebClient();
            string raw = client.DownloadString(WaFFLRosterUri);

            // planned feature to support intelligently determing if a player on the roster + name overrides
            ExtractRosterPlayers(raw);

            foreach (string pattern in HtmlScrubExpressions)
            {
                this.rosterText = Regex.Replace(raw, pattern, @" ", RegexOptions.IgnoreCase);
            }
        }

        /// <summary />
        public static bool IsActive(string search)
        {
            if (instance == null)
            {
                instance = new WaFFLRoster();
            }

            return instance.CheckRosterStatus(search) <  0.3;
        }

        /// <summary />
        private double CheckRosterStatus(string search)
        {
            bool found = this.rosterText.IndexOf(search) != -1;
            if (!found)
            {
                string alternate;
                bool alternateExists = mappings.TryGetValue(search, out alternate);
                if (alternateExists)
                {
                    bool adjustedFound = this.rosterText.IndexOf(alternate) != -1;
                    if (adjustedFound)
                    {
                        return 0.1;
                    }
                }

                return 1.0;
            }

            return 0.0;
        }

        /// <summary />
        private readonly string[] Delimiters = new string[] { "\n" };

        /// <summary />
        private IEnumerable<XElement> ExtractRosterPlayers(string xhtml)
        {           
            // extract the html data table that we are interested in.
            string[] lines = xhtml.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "\t\t\t\t\t\t\t\t\t\t\t<!-- End SIDE1 -->")
                {
                    // start
                }
                else if (lines[i] == "\t\t\t\t\t\t\t\t\t\t\t<!-- Begin SIDE2 -->")
                {

                }
            }

            return null;
        }
    }
}
