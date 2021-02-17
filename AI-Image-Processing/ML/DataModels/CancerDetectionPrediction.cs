using Microsoft.ML.Data;

namespace AI_Image_Processing
{
    public class CancerDetectionPrediction : IOnnxObjectPrediction
    {
        // Use the helper python code to find ModelInput and ModelOutput names
        [ColumnName("Identity:0")]
        public float[] PredictedLabels { get; set; }
    }
}