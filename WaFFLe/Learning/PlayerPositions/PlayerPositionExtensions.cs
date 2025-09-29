using Microsoft.ML.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace WaFFL.Evaluation.Learning.Positions
{
    /// <summary />
    public static class PlayerPositionExtensions
    {
        /// <summary />
        public static FanastyPosition CalculatePosition(this NFLPlayer p)
        {
            int passAttempts = p.GameLog.Sum(g => g.Passing != null ? g.Passing.ATT : 0);
            int rushAttempts = p.GameLog.Sum(g => g.Rushing != null ? g.Rushing.CAR : 0);
            int recievingAttempts = p.GameLog.Sum(g => g.Receiving != null ? g.Receiving.REC : 0);
            int kickAttempts = p.GameLog.Sum(g => g.Kicking != null ? g.Kicking.XPA + g.Kicking.FGA : 0);

            if (passAttempts > rushAttempts && recievingAttempts < 2 && kickAttempts == 0)
            {
                return FanastyPosition.QB;
            }
            else if (passAttempts == 0 && recievingAttempts > rushAttempts && kickAttempts == 0)
            {
                return FanastyPosition.WR;
            }
            else if (passAttempts == 0 && rushAttempts > 0 && recievingAttempts < rushAttempts && kickAttempts == 0)
            {
                return FanastyPosition.RB;
            }
            else if (kickAttempts > 0)
            {
                return FanastyPosition.K;
            }

            return p.Position;
        }

        public static FanastyPosition CalculatePositionML(this NFLPlayer p)
        {
            int value_pass = p.GameLog.Sum(g => g.Passing != null ? g.Passing.ATT : 0);
            int value_rush = p.GameLog.Sum(g => g.Rushing != null ? g.Rushing.CAR : 0);
            int value_catch = p.GameLog.Sum(g => g.Receiving != null ? g.Receiving.REC : 0);
            int value_kick = p.GameLog.Sum(g => g.Kicking != null ? g.Kicking.XPA + g.Kicking.FGA : 0);

            ITransformer trainedModel = WaFFLMachineLearning.__context.Model.Load(WaFFLMachineLearning.ModelFile, out var modelInputSchema);
            var predEngine = WaFFLMachineLearning.__context.Model.CreatePredictionEngine<PositionData, PositionPrediction>(trainedModel);

            VBuffer<float> keys = default;
            predEngine.OutputSchema["PredictedLabel"].GetKeyValues(ref keys);
            var labelsArray = keys.DenseValues().ToArray();

            Dictionary<float, FanastyPosition> fanastyPositions = new Dictionary<float, FanastyPosition>();
            fanastyPositions.Add(0, FanastyPosition.QB);
            fanastyPositions.Add(1, FanastyPosition.RB);
            fanastyPositions.Add(2, FanastyPosition.WR);
            fanastyPositions.Add(3, FanastyPosition.K);

            var position = new PositionData() { Pass = (float)value_pass, Rush = (float)value_rush, Catch = (float)value_catch, Kick = (float)value_kick };
            var result = predEngine.Predict(position);

            var scores = result.Score;
            var maxProb = scores.Max();
            var i = Array.IndexOf(scores, maxProb);
            var predicted = labelsArray[i];

            return fanastyPositions[predicted];
        }
    }

}
