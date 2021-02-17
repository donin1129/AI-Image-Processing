using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.ML;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Reflection;
using AI_Image_Processing;

namespace AI_Image_Processing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private PredictionEngine<ImageInputData, CancerDetectionPrediction> cancerDetectionPredictionEngine;

        //private static readonly string rootDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        //private static readonly string modelsDirectory = Path.Combine(rootDirectory, @"ML\OnnxModels");

        private string status = "";

        private BitmapImage imageSource = new BitmapImage();

        public MainWindow()
        {
            InitializeComponent();
            // Load the model when initialized
            // Encounters last-minute problem when creating executable file -> load model using file dialog
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select ONNX Model";
            if (op.ShowDialog() == true) { LoadModel(op.FileName);}
            
            // Set textblock to show white text
            txtOutput.Foreground = System.Windows.Media.Brushes.White;
        }

        private void btnLoadImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            if (op.ShowDialog() == true)
            {
                imageSource = new BitmapImage(new Uri(op.FileName));
                // If the loaded image is the significantly larger than the ML image parameter (ImageSettings.imageWidth by ImageSettings.imageHeight)
                // It will not process the loaded image (the image might be intended for a whole slide image) and prompt the user
                if ((imageSource.Width > ImageSettings.imageWidth * 1.25) || (imageSource.Height > ImageSettings.imageHeight * 1.25))
                {
                    txtOutput.Text = "Please Load Image";
                    return;
                }
                imgPhoto.Source = imageSource;
                status = "Single Image";
            }
            txtOutput.Text = "";
        }

        private void btnProcessImg_Click(object sender, RoutedEventArgs e)
        {
            // If there is valid image, process the image to return a result
            if (status == "Single Image") {
                Debug.WriteLine("Process Image");
                ProcessImage();
            }
            // If there is no valid image, clear the image output and prompt user
            else
            {
                imgPhoto.Source = new BitmapImage();
                imgMask.Source = new BitmapImage();
                txtOutput.Text = "Please Load Image";
            }
            
        }

        private void btnLoadWSI_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            if (op.ShowDialog() == true)
            {
                imageSource = new BitmapImage(new Uri(op.FileName));
                // If the loaded image is the same as or smaller than the ML image parameter (ImageSettings.imageWidth by ImageSettings.imageHeight)
                // It will not process the loaded image and prompt the user
                if ((imageSource.Width <= ImageSettings.imageWidth) || (imageSource.Height <= ImageSettings.imageHeight))
                {
                    txtOutput.Text = "Please Load Whole Slide";
                    return;
                }
                imgPhoto.Source = imageSource;
                imgMask.Source = new BitmapImage();
                status = "Whole Slide";
            }
            txtOutput.Text = "";
        }

        private void btnProcessWSI_Click(object sender, RoutedEventArgs e)
        {
            // If there is not a generated mask, process the previewed whole slide image
            if (status == "Whole Slide") {
                Debug.WriteLine("Process Whole Slide");
                ProcessWholeSlide(30, 30);
                status = "Whole Slide Preview";
            }
            // If there is a generated mask, toggle the mask on when off
            else if (status == "Whole Slide Preview") {
                imgMask.Opacity = 0.0;
                status = "Whole Slide Preview Off";
            }
            // If there is a generated mask, toggle the mask off when on
            else if (status == "Whole Slide Preview Off") {
                imgMask.Opacity = 0.75;
                status = "Whole Slide Preview";
            }
            // If there is no valid whole slide image, clear the image output and prompt user
            else
            {
                imgPhoto.Source = new BitmapImage();
                txtOutput.Text = "Please Load Whole Slide";
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadModel(string path)
        {
            // Check for an Onnx model exported from Tensorflow to Onnx
            Debug.WriteLine("Loading Cancer Detection Model");

            var cancerDetectionModel = new CancerDetectionModel(path);
            var modelConfigurator = new OnnxModelConfigurator(cancerDetectionModel);

            cancerDetectionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CancerDetectionPrediction>();
        }

        private void ProcessWholeSlide(int ResolutionX, int ResolutionY)
        {
            Bitmap imageDetection = BitmapImage2Bitmap(imageSource);

            // Initialize an array and index count to save the processed prediction[1] (prob of having metasasis) output
            float[] haveCancerPrediction = new float[ResolutionX * ResolutionY];
            int count = 0;

            // If ML Engine Fails to Load
            if (cancerDetectionPredictionEngine == null)
            {
                Debug.WriteLine("ML Engine Failed");
                txtOutput.Text = "ML Engine Failed";
                return;
            }

            // Note: Have better condition handling for resolution input
            // Given the resolution a crop of the whole slide image of size (ImageSettings.imageWidth by ImageSettings.imageHeight) will be processed using the ML model to return a float (prob of having metastasis) appended to the float[]
            for (float x = ImageSettings.imageWidth / 2; x <= imageDetection.Width - ImageSettings.imageWidth / 2; x += (float) (imageDetection.Width - ImageSettings.imageWidth) / (float)(ResolutionX-1))
            {
                for (float y = ImageSettings.imageHeight / 2; y <= imageDetection.Height - ImageSettings.imageHeight / 2; y += (float)(imageDetection.Height - ImageSettings.imageHeight) / (float)(ResolutionY-1))
                {
                    // Crop whole slide image
                    Int32Rect rect = new Int32Rect((int)(x - ImageSettings.imageWidth / 2), (int)(y - ImageSettings.imageHeight / 2), ImageSettings.imageWidth, ImageSettings.imageHeight);
                    CroppedBitmap cb = new CroppedBitmap(Bitmap2BitmapSource(imageDetection), rect);
                    // Predict outcome and set up the float[] to generate the overlay mask
                    var frame = new ImageInputData { Image = BitmapImage2Bitmap(Bitmap2BitmapImage(cb)) };
                    var prediction = DetectUsingModel(frame);
                    haveCancerPrediction.SetValue(prediction[1], count);
                    count++;
                }
            }

            // Convert float[] to a byte[] then to a BitmapSource
            var byteArray = new byte[haveCancerPrediction.Length * 4];
            Buffer.BlockCopy(haveCancerPrediction, 0, byteArray, 0, byteArray.Length);
            BitmapSource overlayImg= FromArray(byteArray, ResolutionX, ResolutionY, 1);
            
            // Scale the Bitmap mask to cover the whole slide image
            var targetBitmapImage = new TransformedBitmap(overlayImg, new ScaleTransform((imageDetection.Width)/ ResolutionX, (imageDetection.Height) / ResolutionY));

            // Set the bitmap to display
            imgMask.Source = targetBitmapImage;
        }

        private void ProcessImage()
        {
            Bitmap imageDetection = BitmapImage2Bitmap(imageSource);

            // If ML Engine Fails to Load
            if (cancerDetectionPredictionEngine == null)
            {
                Debug.WriteLine("ML Engine Failed");
                txtOutput.Text = "ML Engine Failed";
                return;
            }

            var frame = new ImageInputData { Image = imageDetection };

            Debug.WriteLine("Detect Objects Using Model");
            var prediction = DetectUsingModel(frame);

            // Text output to textblock
            // Note: Label 1 means having metastasis and Label 0 means not having metastasis
            txtOutput.Text = "Having metastasis: \n";
            txtOutput.Text += Math.Round(prediction[1] * 100, 4) + "%\n\n";
            txtOutput.Text += "Not having metastasis: \n";
            txtOutput.Text +=  Math.Round(prediction[0] * 100, 4) + "%\n";

            // Debug: print each predicted values
            // foreach (var result in prediction){ Debug.WriteLine($"BoxDescription: {result}");}
        }

        // Use Model to Predict Feedback from ImageInputData (a single image)
        public float[] DetectUsingModel(ImageInputData imageInputData)
        {
            var labels = cancerDetectionPredictionEngine?.Predict(imageInputData).PredictedLabels;
            return labels;
        }

        // Helper Function to Convert byte[] to BitmapSource
        public static BitmapSource FromArray(byte[] data, int w, int h, int ch)
        {
            PixelFormat format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Bgr24; //RGB
            if (ch == 4) format = PixelFormats.Bgr32; //RGB + alpha

            WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data, ch * w, 0);

            return wbm;
        }

        // Helper Function to Convert CroppedBitmap (BitmapSource) to BitmapImage
        private BitmapImage Bitmap2BitmapImage(BitmapSource cb)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();
            encoder.Frames.Add(BitmapFrame.Create(cb));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            bImg.BeginInit();
            bImg.StreamSource = memoryStream;
            bImg.EndInit();

            return bImg;
        }

        // Helper Function to Convert BitmapImage to Bitmap
        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new System.Drawing.Bitmap(outStream);
                return new Bitmap(bitmap);
            }
        }

        // Helper Function to Convert Bitmao to BitmapSource
        public static BitmapSource Bitmap2BitmapSource(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

    }
}
