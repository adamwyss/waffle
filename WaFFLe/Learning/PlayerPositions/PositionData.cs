using Microsoft.ML.Data;

namespace WaFFL.Evaluation.Learning.Positions
{
    public class PositionData
    {
        [LoadColumn(0)]
        public float Label;

        [LoadColumn(1)]
        public float Pass;

        [LoadColumn(2)]
        public float Rush;

        [LoadColumn(3)]
        public float Catch;

        [LoadColumn(4)]
        public float Kick;
    }
}
