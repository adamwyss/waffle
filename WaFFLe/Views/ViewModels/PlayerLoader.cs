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
                { FanastyPosition.QB, season.ReplacementValue.QB },
                { FanastyPosition.RB, season.ReplacementValue.RB },
                { FanastyPosition.WR, season.ReplacementValue.WR },
                { FanastyPosition.K, season.ReplacementValue.K },
                { FanastyPosition.DST, season.ReplacementValue.DST }
            };
        }

        public IEnumerable<PlayerViewModel> GetViewModels()
        {
            var results = this._season.GetAll().Select(ConvertToViewModel);
            return results;
        }

        private PlayerViewModel ConvertToViewModel(NFLPlayer player)
        {
            int points = 0;
            int games = 0;
            int por = 0;
            int wpor = 0;
            double mean = 0;
            double standardDeviation = 0;
            double variationCoefficient = 0;

            if (player.Position == FanastyPosition.DST)
            {
                points = player.Team.ESPNTeamDefense.Estimate_Points();
                games = _season.GetAllPlayers().Where(p => p.Team == player.Team).Max(p => p.GamesPlayed());

                if (games > 0)
                {
                    por = (points / games) - _season.ReplacementValue.DST;

                    // fudge with the number to make the scores relative to the players who have played
                    // less than 3 games in the beginning of the season.
                    double factor = 1.0;
                    if (games == 1) factor = 0.333;
                    if (games == 2) factor = 0.666;
                    wpor = (int)Math.Round(por * factor, 0);
                }
            }
            else
            {
                points = player.FanastyPoints();
                games = player.GamesPlayed();

                if (games > 0)
                {
                    int replacementScore = _replacementScores[player.Position];

                    por = (points / games) - replacementScore;
                    wpor = (player.FanastyPointsInRecentGames(3) / 3) - replacementScore;

                    mean = player.FanastyPointsPerGame().Mean();

                    standardDeviation = player.FanastyPointsPerGame().StandardDeviation();
                }
            }

            if (mean > 0)
            {
                variationCoefficient = standardDeviation / mean;
            }
            else
            {
                variationCoefficient = 0;
            }

            PlayerViewModel vm = new PlayerViewModel(player);
            vm.PointsOverReplacement = por;
            vm.WeightedPointsOverReplacement = wpor;
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
