using System;
using System.Net;
using System.Text.RegularExpressions;

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
            this.rosterText = client.DownloadString(WaFFLRosterUri);

            foreach (string pattern in HtmlScrubExpressions)
            {
                this.rosterText = Regex.Replace(this.rosterText, pattern, @" ", RegexOptions.IgnoreCase);
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
                        { "Philip", "Phillip" },
                        { "LaDainian", "Ladanian" },
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
                        { "Torrey", "Torrie" },
                        { "C.J.", "CJ" },
                        { "Joique", "Joqui" },
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
    }
}
