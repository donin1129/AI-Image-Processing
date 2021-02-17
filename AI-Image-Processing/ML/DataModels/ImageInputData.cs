using Microsoft.ML.Transforms.Image;
using System.Drawing;

namespace AI_Image_Processing
{
    public struct ImageSettings
    {
        // Refer to the training input of the ML model
        public const int imageHeight = 96;
        public const int imageWidth = 96;
    }

    public class ImageInputData
    {
        [ImageType(ImageSettings.imageHeight, ImageSettings.imageWidth)]
        public Bitmap Image { get; set; }
    }
}