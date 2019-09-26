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

            return 0.3 > instance.CheckRosterStatus(search);
        }

        /// <summary />
        private double CheckRosterStatus(string search)
        {
            // corect NY abbrev.
            if (search.StartsWith("NY", StringComparison.InvariantCulture))
            {
                search = "New York" + search.Substring(2);
            }

            bool found = this.rosterText.IndexOf(search) != -1;
            if (!found)
            {
                // correct common miss spellings
                string[,] corrections = new string[,]
                    {
                    //  { "ESPN", "WaFFL" },
                        { "Philip", "Phillip" },
                        { "Malcom", "Malcolm" },
                        { "Matthew", "Matt" },
                        { "Griffin", "Griffen" },
                        { "Stevan", "Steven" },
                        { "Leshoure", "LaShoure" },
                        { "Bolden", "Boldin" },
                        { "Mathews", "Matthews" },
                        { "Isaac", "Issac" },
                        { "A.J.", "AJ" },
                        { "Zuerlein", "Zuerlien" },
                        { "Stephen", "Stephan" },
                        { "Darrius Heyward-Bey", "Darius Hayward-Bey" },
                        { "C.J.", "CJ" },
                        { "Jeffery", "Jeffrey" },
                        { "Blackmon", "Blackman" },
                        { "Eric", "Erik" },
                        { "Le'Veon Bell", "LeVeon Bell" },
                        { "Shane Vereen", "Shane Vareen" },
                        { "Steve Smith Sr.", "Steve Smith" },
                        { "Cordarrelle Patterson", "Cordarelle Patterson" },
                        { "T.Y. Hilton", "TY Hilton" },
                        { "Branden Oliver", "Brandon Oliver" },
                        { "Jordan Matthews", "Jordan Mathews" },
                        { "Austin Seferian-Jenkins", "Austin Safarian-Jenkens" },
                        { "Julian Edelman", "Julien Edelman" },
                        { "Michael Floyd", "Michael Foyd" },
                        { "Brandon LaFell", "Brando LaFell" },
                        { "T.J. Yeldon", "TJ Yeldon" },
                        // 2018
                        { "Todd Gurley II", "Todd Gurley" },
                        { "James Conner", "James Connor" },
                        { "Phillip Lindsay", "Philip Lindsay" },
                        { "Will Fuller V", "Will Fuller" },
                        { "Devonta Freeman", "Davonta Freeman" },
                        { "Davante Adams", "Davonte Adams" },
                        // 2019
                        { "Saquon Barkley", "Saquan Barclay" },
                        { "Deshaun Watson", "DeShaun Watson" },
                        { "Odell Beckham", "O'Dell Beckham Jr" },
                        { "Patrick Mahomes", "Pat Mahomes" },
                    };

                for (int i = 0; i < corrections.Length / 2; i++)
                {
                    string value = corrections[i, 0];

                    int pos = search.IndexOf(value);
                    if (pos >= 0)
                    {
                        string adjustedSearch = search.Substring(0, pos) + corrections[i, 1] + search.Substring(pos + value.Length);

                        bool adjustedFound = this.rosterText.IndexOf(adjustedSearch) != -1;
                        if (adjustedFound)
                        {
                            return 0.1;
                        }
                      
                        break;
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
