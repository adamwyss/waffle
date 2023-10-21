using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class WaFFLRoster : IWaFFLRoster     
    {
        private static readonly Dictionary<string, string> mappings = new Dictionary<string, string>()
        {
        //  { "Correct Spelling", "WaFFL Misspelling" },
            // 2019
            { "Patrick Mahomes", "Pat Mahomes" },
            { "Odell Beckham", "O'Dell Beckham Jr" },
            { "Le'Veon Bell", "LeVeon Bell" },
            { "Phillip Lindsay", "Philip Lindsay" },
            { "Devonta Freeman", "Davonta Freeman" },
            { "T.Y.Hilton", "TY Hilton" },
            { "Matthew Stafford", "Matt Stafford" },
            { "Deshaun Watson", "DeShaun Watson" },
            { "Alshon Jeffery", "Alshon Jeffrey" },
            { "Stephen Gostkowski", "Stephan Gostkowski" },
            { "San Francisco 49ers", "San Fransciso 49ers" },
            { "D.J. Moore", "DJ Moore " },
            { "DeVante Parker", "DaVante Parker" },
            //2020
            { "J.K. Dobbins", "JK Dobbins" },
            { "Adam Thielen", "Adam Theilen" },
            { "D.K. Metcalf", "DK Metcalf" },
            //2021
            { "A.J. Brown", "AJ Brown" },
            { "Ja'Marr Chase", "JaMarr Chase" },
            { "Cordarrelle Patterson", "Cordarelle Patterson" },
            { "Saquon Barkley", "Saquon Barclay" },
            { "Mike Williams", "Mike Wiliams"},
            { "Damien Harris", "Damian Harris" },
            { "Chuba Hubbard", "Chubba Hubbard" },
            //2022
            { "Tua Tagovailoa", "Tua Tagovialoa"},
            { "Michael Pittman Jr.", "Michael Pittman" },
            { "DeAndre Hopkins", "De'Andre Hopkins" },
            { "Joshua Palmer", "Joshua Palmer" },
            { "Brian Robinson Jr.", "Brian Robinson" },
            { "Kenneth Walker III", "Kenneth Walker"},
            //2023
            { "Zack Moss", "Zach Moss"},
            { "DeVonta Smith", "DaVonta Smith" },
            { "Ka'imi Fairbairn", "Kai'imi Faribairn" },
            { "C.J. Stroud", "CJ Stroud" },
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
        private string rosterText;

        /// <summary />
        public WaFFLRoster()
        {
            WebClient client = new WebClient();
            string raw = client.DownloadString(WaFFLRosterUri);

            foreach (string pattern in HtmlScrubExpressions)
            {
                this.rosterText = Regex.Replace(raw, pattern, @" ", RegexOptions.IgnoreCase);
            }
        }

        /// <summary />
        public string CheckRosterStatus(string search)
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
                        return "n/a";
                    }
                }

                return null;
            }

            return "n/a";
        }
    }
}
