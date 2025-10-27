using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
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
            var results = this._season.GetAllPlayers().Where(p => p.IsRelevant() && p.GetPositionOrBestGuess() != FanastyPosition.UNKNOWN).Select(ConvertToViewModel);
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

            var position = player.GetPositionOrBestGuess();
            if (games > 0 && position != FanastyPosition.UNKNOWN)
            {
                int replacementScore = _replacementScores[position];

                por = (points / games) - replacementScore;
                wpor = (player.FanastyPointsInRecentGames(3) / 3) - replacementScore;
                cpor = GetRunningAverageOfGamePoints(player) - replacementScore;

                mean = player.FanastyPointsPerGame().Mean();
                standardDeviation = player.FanastyPointsPerGame().StandardDeviation();
                variationCoefficient = mean > 0 ? standardDeviation / mean : 0;
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
            if (games.Count < 3)
            {
                return (int)games.Average(g => g.GetFanastyPoints());
            }

            var points = games.Select(g => g.GetFanastyPoints()).ToList();
            int count = points.Count;
            int lastIndex = count - 1;
            List<double> avgScores = new List<double>();
            for (int i = 0; i < count; i++)
            {
                int before = i == 0 ? lastIndex : i - 1;
                int after = i == lastIndex ? 0 : i + 1;

                int sum = points[before] + points[i] + points[after];
                avgScores.Add(sum / 3);
            }

            return (int)avgScores.Average();
        }

        private int RoundToInt(double value)
        {
            return Convert.ToInt32(Math.Round(value, 0));
        }

    }
}
