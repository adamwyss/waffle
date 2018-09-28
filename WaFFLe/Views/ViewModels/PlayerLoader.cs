using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaFFL.Evaluation.Views.ViewModels
{
    public class PlayerLoader<T> where T: Item
    {
        private FanastyPosition _postition;
        private int _replacementLevel;
        private Func<NFLPlayer, T> _factory;

        public PlayerLoader(FanastyPosition position, int replacementLevel, Func<NFLPlayer, T> factory)
        {
            _postition = position;
            _replacementLevel = replacementLevel;
            _factory = factory;
        }

        public IEnumerable<T> GetViewModels(FanastySeason season)
        {
            Collection<T> results = new Collection<T>();

            foreach (NFLPlayer player in season.GetAll(_postition))
            {
                T vm = _factory(player);

                int points = player.FanastyPoints();
                int games = player.GamesPlayed();

                if (games > 0)
                {
                    vm.PointsOverReplacement = (points / games) - _replacementLevel;
                    vm.WeightedPointsOverReplacement = (player.FanastyPointsInRecentGames(3) / 3) - _replacementLevel;

                    double mean = player.FanastyPointsPerGame().Mean();

                    double standardDeviation = player.FanastyPointsPerGame().StandardDeviation();

                    vm.Mean = Convert.ToInt32(Math.Round(mean, 0));
                    vm.StandardDeviation = Convert.ToInt32(Math.Round(standardDeviation, 0));

                    if (mean > 0)
                    {
                        vm.CoefficientOfVariation = Convert.ToInt32(Math.Round(standardDeviation / mean * 100, 0));
                    }
                    else
                    {
                        vm.CoefficientOfVariation = 0;
                    }
                }

                vm.FanastyPoints = points;
                vm.TotalBonuses = player.TotalBonuses();

                results.Add(vm);
            }

            return results;
        }

    }
}
