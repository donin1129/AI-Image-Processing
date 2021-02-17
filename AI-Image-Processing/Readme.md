# PreciPoint Challenge: AI-Image-Processing

**Author: Zhetao Dong**

This program is expected to provide a .NET Core WPF application that would enable a user to load an image, process it in parts or as a whole and displays the results in the application UI by integrating .NET / C# / ONNX runtime. 

Using this application, a user can load an image or a whole-slide image to be processed as a whole or in parts respectively by applying the same neural network trained using Tensorflow and deployed as ONNX framework. 

## Version 1
Key Improvements:
- Provide fundamental single image processing function processed as a whole using the application. 

For the first version, I focused on setting up the workflow, familiarizing the coding environment and getting the model deployed correctly with the most basic function. 

I started by learning from this example https://github.com/dotnet/machinelearning-samples/tree/master/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx. 

After encountering several road blocks and realizing some necessary structural improvements, I switched to this example https://github.com/dotnet/machinelearning-samples/tree/master/samples/csharp/end-to-end-apps/ObjectDetection-Onnx. 

**Notes:** 
Exporting ONNX model from a Tensorflow model is not as intuitive. 
> For me, I used keras.layers.LeakyReLU, which could not be smoothly exported as a ONNX model. (It is a good practice to test export the model after setting up the neural network trained using a different environment). 

Deploying ONNX models requires input and output names of the network.
> This took me a lot of time to debug, since the error message was not very intuitively and pointed towards the wrong direction. In the end, I was able to identify the problem and set up a short python code to load the ONNX file and read the layer information and input output names. (It is absolutely necessary to get the input output names of the ONNX network). 

The input data provided to the network is not intuitive (compare to running on a python notebook). 
> I noticed that the deployed ONNX model is not producing the same result compare to the Tensorflow model. I set up the testing ground by loading ONNX model back to Tensorflow to compare with the original Tensorflow model. In the end, the problem was that when extracting pixel values, parameter inputleavePixelColors need to be set to "true" in my occasion. 

## Version 2 - Current
Key Improvements:
- Provide large FOV image processing function using the application. 
- Improved UI and operating logic

Having more experience working with the environment and successfully deploying the ONNX model, for the second version, I focused on organizing the UI/UX and building the extra function to process whole-slide images by dividing them to smaller tiles to be analyzed by the same network. It is like applying a convolutional kernel to the whole-slide image. 

**Notes:** 
The application can not load a multi-resolution mega-pixel whole-slide file yet. 
> The intention was to have the application process multi-resolution mega-pixel images. However, I looked for several different packages but no one worked well for me. The openslideNET package caused an error saying that an essential dll file could not be found. In the end, I had to compromise by loading a large FOV (field of view) image with higher resolution to demonstrate the workflow. 

There is no prompt for a user to set up the resolution in the UI for the bitmap output after processing the "whole-slide" image. 
> The resolution of the output Bitmap from processing the "whole-slide" image is set to be 30 pixels by 30 pixels. This means the "whole-slide" image will be divided to 30 by 30 tiles consist of 96 pixels by 96 pixels images for the network to output the probability of having metastasis in each tile. In the source code, the resolution can be changed easily by providing different parameters to the ProcessWholeSlide function. However, in the executable file, the user cannot reset it using existing UI. 

## Outlook
Possible Future Improvements:
- Support multi-resolution mega-pixel whole-slide files. 
- More interactive UI and UX (such as for viewing whole-slide images). 
- More testing and better error handling. 
- Better visualization. 
- Load machine learning model during initialization. 
> I encountered a last-minute problem that the release build cannot correctly allocate onnx file. 