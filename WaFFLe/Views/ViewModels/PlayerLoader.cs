using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace WaFFL.Evaluation.Views.ViewModels
{
    public class PlayerLoader
    {
        private FanastySeason _season;
        private Dictionary<FanastyPosition, int> _replacementScores;

        public PlayerLoader(FanastySeason season)
        {
            _season = season;
            _replacementScores = new Dictionary<FanastyPosition, int>()
            {
                { FanastyPosition.QB, season.ReplacementValue?.QB ?? 0 },
                { FanastyPosition.RB, season.ReplacementValue?.RB ?? 0 },
                { FanastyPosition.WR, season.ReplacementValue?.WR ?? 0 },
                { FanastyPosition.K, season.ReplacementValue?.K ?? 0 },
                { FanastyPosition.DST, season.ReplacementValue?.DST ?? 0 },
            };
        }

        public IEnumerable<PlayerViewModel> GetViewModels()
        {
            var results = this._season.GetAllPlayers().Where(p => p.GamesPlayed() > 0).Select(ConvertToViewModel);
            return results;
        }

        private PlayerViewModel ConvertToViewModel(NFLPlayer player)
        {
            int points = 0;
            int games = 0;
            int por = 0;
            int wpor = 0;
            int cpor = 0;
            double mean = 0;
            double standardDeviation = 0;
            double variationCoefficient = 0;

            points = player.FanastyPoints();
            games = player.GamesPlayed();


            if (games > 0 && player.Position != FanastyPosition.UNKNOWN)
            {
                int replacementScore = _replacementScores[player.Position];

                por = (points / games) - replacementScore;
                wpor = (player.FanastyPointsInRecentGames(3) / 3) - replacementScore;
                cpor = GetRunningAverageOfGamePoints(player) - replacementScore;

                mean = player.FanastyPointsPerGame().Mean();
                standardDeviation = player.FanastyPointsPerGame().StandardDeviation();
                variationCoefficient = standardDeviation / mean;
            }

            PlayerViewModel vm = new PlayerViewModel(player);
            vm.PointsOverReplacement = por;
            vm.WeightedPointsOverReplacement = wpor;
            vm.ConsistentPointsOverReplacement = cpor;
            vm.FanastyPoints = points;
            vm.TotalBonuses = player.TotalBonuses();
            vm.Mean = RoundToInt(mean);
            vm.StandardDeviation = RoundToInt(standardDeviation);
            vm.CoefficientOfVariation = RoundToInt(variationCoefficient * 100);
            return vm;
        }

        private int GetRunningAverageOfGamePoints(NFLPlayer player)
        {
            var games = player.GameLog.OrderBy(g => g.Week).ToList();
            List<int> avgScores = new List<int>();
            for (int i = 0; i < games.Count; i++)
            {
                int avg = 0;
                if (i == 0)
                {
                    avg = games[i].GetFanastyPoints();
                }
                else if (i == 1)
                {
                    avg = (games[i].GetFanastyPoints() + games[i - 1].GetFanastyPoints());
                    avg /= 2;
                }
                else if (i >= 2)
                {
                    avg = games[i].GetFanastyPoints() + games[i - 1].GetFanastyPoints() + games[i - 2].GetFanastyPoints();
                    avg /= 3;
                }

                avgScores.Add(avg);
            }
            return (int)avgScores.Average();
        }

        private int RoundToInt(double value)
        {
            return Convert.ToInt32(Math.Round(value, 0));
        }

    }
}
