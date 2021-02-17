namespace AI_Image_Processing
{
    public class CancerDetectionModel : IOnnxModel
    {
        public string ModelPath { get; private set; }

        // Use the helper python code to find ModelInput and ModelOutput names
        // Use the helper python code to test if the converted ONNX file is not corrupted and produce bad result
        public string ModelInput { get; } = "conv2d_input:0";
        public string ModelOutput { get; } = "Identity:0";

        public CancerDetectionModel(string modelPath)
        {
            ModelPath = modelPath;
        }
    }
}
