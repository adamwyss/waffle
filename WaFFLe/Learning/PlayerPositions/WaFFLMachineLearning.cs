using Microsoft.ML.Data;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaFFL.Evaluation.Learning.Positions
{
    public static class WaFFLMachineLearning
    {
        public static MLContext __context = new MLContext(seed: 0);

        public static string TrainingFile = "C:\\Source\\waffle\\WaFFLe\\Learning\\Positions\\Data\\training.txt";
        public static string TestFile = "C:\\Source\\waffle\\WaFFLe\\Learning\\Positions\\verification.txt";
        public static string ModelFile = "C:\\Source\\waffle\\trainingdata\\position-model.zip";

        public static void BuildTrainEvaluateAndSave(MLContext context)
        {
            var trainingDataView = context.Data.LoadFromTextFile<PositionData>(TrainingFile, hasHeader: true);
            var testDataView = context.Data.LoadFromTextFile<PositionData>(TestFile, hasHeader: true);

            var dataProcessPipeline = context.Transforms.Conversion
                .MapValueToKey(outputColumnName: "KeyColumn", inputColumnName: nameof(PositionData.Label))
                .Append(context.Transforms.Concatenate("Features", nameof(PositionData.Pass),
                                                                   nameof(PositionData.Rush),
                                                                   nameof(PositionData.Catch),
                                                                   nameof(PositionData.Kick))
                .AppendCacheCheckpoint(context));

            var trainer = context.MulticlassClassification.Trainers
                .SdcaMaximumEntropy(labelColumnName: "KeyColumn", featureColumnName: "Features")
                .Append(context.Transforms.Conversion.MapKeyToValue(outputColumnName: nameof(PositionData.Label), inputColumnName: "KeyColumn"));

            var trainingPipeline = dataProcessPipeline.Append(trainer);

            Console.WriteLine("=============== Training the model ===============");
            ITransformer trainedModel = trainingPipeline.Fit(trainingDataView);

            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====");
            var predictions = trainedModel.Transform(testDataView);
            var metrics = context.MulticlassClassification.Evaluate(predictions, "Label", "Score");

            PrintMultiClassClassificationMetrics(trainer.ToString(), metrics);

            context.Model.Save(trainedModel, trainingDataView.Schema, ModelFile);
            Console.WriteLine("The model is saved to {0}", ModelFile);
        }

        public static void PrintMultiClassClassificationMetrics(string name, MulticlassClassificationMetrics metrics)
        {
            Console.WriteLine($"************************************************************");
            Console.WriteLine($"*    Metrics for {name} multi-class classification model   ");
            Console.WriteLine($"*-----------------------------------------------------------");
            Console.WriteLine($"    AccuracyMacro = {metrics.MacroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
            Console.WriteLine($"    AccuracyMicro = {metrics.MicroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
            Console.WriteLine($"    LogLoss = {metrics.LogLoss:0.####}, the closer to 0, the better");
            Console.WriteLine($"    LogLoss for class 1 = {metrics.PerClassLogLoss[0]:0.####}, the closer to 0, the better");
            Console.WriteLine($"    LogLoss for class 2 = {metrics.PerClassLogLoss[1]:0.####}, the closer to 0, the better");
            Console.WriteLine($"    LogLoss for class 3 = {metrics.PerClassLogLoss[2]:0.####}, the closer to 0, the better");
            Console.WriteLine($"************************************************************");
        }

        public static void TestPredictions(MLContext context)
        {
            ITransformer trainedModel = context.Model.Load(ModelFile, out var modelInputSchema);

            var predEngine = context.Model.CreatePredictionEngine<PositionData, PositionPrediction>(trainedModel);

            VBuffer<float> keys = default;
            predEngine.OutputSchema["PredictedLabel"].GetKeyValues(ref keys);
            var labelsArray = keys.DenseValues().ToArray();

            Dictionary<float, string> FanastyPositions = new Dictionary<float, string>();
            FanastyPositions.Add(0, "QB");
            FanastyPositions.Add(1, "RB");
            FanastyPositions.Add(2, "WR");
            FanastyPositions.Add(3, "K");

            Console.WriteLine("=====Predicting using model====");

            int total = 0;
            int wrong = 0;

            var testDataView = context.Data.LoadFromTextFile<PositionData>(TestFile, hasHeader: true);
            var preview = testDataView.Preview();
            foreach (var row in preview.RowView)
            {
                var expected = (float)row.Values[0].Value;
                var position1 = new PositionData() { Pass = (float)row.Values[1].Value, Rush = (float)row.Values[2].Value, Catch = (float)row.Values[3].Value, Kick = (float)row.Values[4].Value };
                var resultprediction1 = predEngine.Predict(position1);

                var r = resultprediction1.Score;
                var m = r.Max();
                var i = Array.IndexOf(r, m);
                var predicted = labelsArray[i];

                total++;
                if (predicted != Convert.ToInt32(expected))
                {
                    wrong++;
                }

                Console.WriteLine($"Actual: {FanastyPositions[expected]}.     Predicted label and score:  {FanastyPositions[labelsArray[0]]}: {resultprediction1.Score[0]:0.####}");
                Console.WriteLine($"                                            {FanastyPositions[labelsArray[1]]}: {resultprediction1.Score[1]:0.####}");
                Console.WriteLine($"                                            {FanastyPositions[labelsArray[2]]}: {resultprediction1.Score[2]:0.####}");
                Console.WriteLine($"                                            {FanastyPositions[labelsArray[3]]}: {resultprediction1.Score[3]:0.####}");
                Console.WriteLine($"   ACTUAL DATA Pass({position1.Pass}) Rush({position1.Rush}) Catch({position1.Catch}) Kick({position1.Kick})");
                Console.WriteLine();
                Console.WriteLine();


            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"SUMMARY: {total - wrong} / {total} = {(total - wrong) / total * 100}%      {wrong} incorrect ");
        }

        public static void VerifyResultsWithRealData(FanastySeason season)
        {
            season.GetAllPlayers().Where(p => p.Position != FanastyPosition.DST).ToList().ForEach(player =>
            {
                var x = player.CalculatePosition();
                var y = player.CalculatePositionML();
                bool same = x == y;
                bool correct = y == player.Position;
                Console.WriteLine("{4} {0} H:{1} ML:{2} A:{3}", player.Name, x, y, player.Position, same && correct ? "     " : " *** ");
            });
        }

        public static void GetTrainingData(FanastySeason season)
        {
            int[] training = new int[4];
            var players = season.GetAllPlayers();
            using (StreamWriter writer = new StreamWriter(@"C:\\Source\\waffle\trainingdata\position-training.txt"))
            using (StreamWriter writer2 = new StreamWriter(@"C:\\Source\\waffle\trainingdata\position-test.txt"))
            {
                writer.WriteLine("#Label\tQB_ATT\tRB_ATT\tWR_ATT\tK_ATT");
                writer2.WriteLine("#Label\tQB_ATT\tRB_ATT\tWR_ATT\tK_ATT");

                foreach (var player in players)
                {
                    var pos = player.Position;
                    if (pos != FanastyPosition.UNKNOWN && pos != FanastyPosition.DST)
                    {
                        int value_pos = (int)pos - 1;
                        int value_pass = player.GameLog.Sum(g => g.Passing != null ? g.Passing.ATT : 0);
                        int value_rush = player.GameLog.Sum(g => g.Rushing != null ? g.Rushing.CAR : 0);
                        int value_catch = player.GameLog.Sum(g => g.Receiving != null ? g.Receiving.REC : 0);
                        int value_kick = player.GameLog.Sum(g => g.Kicking != null ? g.Kicking.XPA + g.Kicking.FGA : 0);

                        var x = training[value_pos] % 10;
                        if (x == 3 || x == 7)
                        {
                            writer2.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", value_pos, value_pass, value_rush, value_catch, value_kick);
                        }
                        else
                        {
                            writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", value_pos, value_pass, value_rush, value_catch, value_kick);
                        }

                        training[value_pos] = training[value_pos] + 1;
                    }
                }
            }
        }
    }


}
