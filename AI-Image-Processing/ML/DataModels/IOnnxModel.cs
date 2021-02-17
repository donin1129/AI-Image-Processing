using System;
using System.Collections.Generic;
using System.Text;

namespace AI_Image_Processing
{
    public interface IOnnxModel
    {
        string ModelPath { get; }
        string ModelInput { get; }
        string ModelOutput { get; }
    }

    public interface IOnnxObjectPrediction
    {
        float[] PredictedLabels { get; set; }
    }
}
