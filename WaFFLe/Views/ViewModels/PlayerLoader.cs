using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

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
                cpor = PredictNextValue(player) - replacementScore;

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

        private double ExponentialMovingAverage(List<int> data, double alpha = 0.3)
        {
            // Smooths data while responding quickly to change.
            // Where alpha is a smoothing factor(0 < alpha ≤ 1).
            // Higher alpha → reacts faster
            // Lower alpha → smoother, slower reaction
            // This EMA value will track your trend and should be close to the next data point, even when the series is noisy.
            if (data.Count == 0) return 0;
            double ema = data[0];
            for (int i = 1; i < data.Count; i++)
                ema = alpha * data[i] + (1 - alpha) * ema;
            return ema;
        }

        private int PredictNextValue(NFLPlayer player)
        {
            var games = player.GameLog.OrderBy(g => g.Week).ToList();
            if (games.Count < 3)
            {
                return (int)games.Average(g => g.GetFanastyPoints());
            }

            var points = games.Select(g => g.GetFanastyPoints()).ToList();
            double shortTerm = ExponentialMovingAverage(points, 0.5);
            double longTerm = ExponentialMovingAverage(points, 0.2);

            // This “dual EMA” smooths the noise but still follows direction changes —
            // similar to what’s used in financial trend analysis(MACD - like smoothing).
            double ema = (shortTerm * 0.3) + (longTerm * 0.7);
            return RoundToInt(ema);
        }

        double WeightedMovingAverage(NFLPlayer player)
        {
            var games = player.GameLog.OrderBy(g => g.Week).ToList();
            if (games.Count < 3)
            {
                return (int)games.Average(g => g.GetFanastyPoints());
            }

            var last3 = games.Skip(games.Count - 3).Select(g => g.GetFanastyPoints()).ToList();
            double w1 = 1, w2 = 2, w3 = 3;
            var wma = (last3[0] * w1 + last3[1] * w2 + last3[2] * w3) / (w1 + w2 + w3);
            return RoundToInt(wma);
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
