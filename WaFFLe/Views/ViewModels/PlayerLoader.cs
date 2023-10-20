using System;
using System.Collections.Generic;
using System.Linq;

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

                mean = player.FanastyPointsPerGame().Mean();
                if (mean > 0)
                {
                    standardDeviation = player.FanastyPointsPerGame().StandardDeviation();
                    variationCoefficient = standardDeviation / mean;


                    double multiplier = 1 - Math.Min(variationCoefficient, 1);

                    cpor = (RoundToInt(points * multiplier) / games) - replacementScore;
                }

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

        private int RoundToInt(double value)
        {
            return Convert.ToInt32(Math.Round(value, 0));
        }

    }
}
