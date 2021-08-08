using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TransferLearningTF
{
    partial class Program
    {
        static readonly string _assetsPath = Path.Combine(Environment.CurrentDirectory, "assets");
        static readonly string _imagesFolder = Path.Combine(_assetsPath, "images");
        static readonly string _trainTagsTsv = Path.Combine(_imagesFolder, "tags.tsv");
        static readonly string _testTagsTsv = Path.Combine(_imagesFolder, "test-tags.tsv");
        //static readonly string _predictSingleImage = Path.Combine(_imagesFolder, "toaster3.jpg");
        // static readonly string _predictSingleImage = Path.Combine(_imagesFolder, "teddy4.jpg");
        static readonly string _predictSingleImage = Path.Combine(_imagesFolder, "food2.jpg");

        static readonly HashSet<string> images = new HashSet<string>
        {
          Path.Combine(_imagesFolder, "food1.jpg"),
          Path.Combine(_imagesFolder, "food2.jpg"),
          Path.Combine(_imagesFolder, "food3.jpg"),
          Path.Combine(_imagesFolder, "food4.jpg"),
          Path.Combine(   _imagesFolder, "food5.jpg" ),
            Path.Combine( _imagesFolder, "toaster.jpg" ),
            Path.Combine( _imagesFolder, "toaster2.png" ),
            Path.Combine( _imagesFolder, "toaster3.jpg" ),
            Path.Combine( _imagesFolder, "teddy2.jpg" ),
            Path.Combine( _imagesFolder, "teddy3.jpg" ),
            Path.Combine( _imagesFolder, "teddy4.jpg" ),
Path.Combine(             _imagesFolder, "broccoli.jpg" ),
Path.Combine(             _imagesFolder, "broccoli2.jpg" ),
            Path.Combine( _imagesFolder, "headsetMy.jpg" ),
            Path.Combine( _imagesFolder, "headset1.jfif" ),
            Path.Combine( _imagesFolder, "headset2.jfif" ),
            Path.Combine( _imagesFolder, "headset.jfif" ),
            Path.Combine( _imagesFolder, "toaster.jpg" ),

                 };

        static readonly string _inceptionTensorFlowModel = Path.Combine(_assetsPath, "inception", "tensorflow_inception_graph.pb");

        static void Main(string[] args)
        {
            try
            {

                MLContext mLContext = new MLContext();
                var model = GenerateModel(mLContext);

                mLContext.Model.Save(model.Item1, model.Item2.Schema, "model.zip");
                //  ClassifySingleImage(mLContext, model);
                Console.WriteLine();
                ClassifyManyImages(mLContext, model.Item1);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void DisplayResults(IEnumerable<ImagePrediction> imagePrediction)
        {
            foreach (var prediction in imagePrediction)
            {
                Console.WriteLine($"Image: {Path.GetFileName(prediction.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
            }
        }

        public static IEnumerable<ImageData> ReadFromTsv(string file, string folder)
        {
            return File.ReadAllLines(file)
                .Select(line => line.Split('\t'))
                .Select(line => new ImageData
                {
                    ImagePath = Path.Combine(folder, line[0])
                });
        }

        public static void ClassifySingleImage(MLContext mLContext, ITransformer model)
        {
            var imageData = new ImageData
            {
                ImagePath = _predictSingleImage
            };
            var predictor = mLContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
            var prediction = predictor.Predict(imageData);

            Console.WriteLine($"Image: {Path.GetFileName(imageData.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
        }

        public static void ClassifyManyImages(MLContext mLContext, ITransformer model)
        {
            foreach (var image in images)
            {
                var imageData = new ImageData
                {
                    ImagePath = image
                };

                var predictor = mLContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
                var prediction = predictor.Predict(imageData);

                Console.WriteLine($"Image: {Path.GetFileName(imageData.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
            }
        }

        public static (ITransformer, IDataView) GenerateModel(MLContext mLContext)
        {
            IEstimator<ITransformer> pipeline = mLContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: _imagesFolder, inputColumnName: nameof(ImageData.ImagePath))
                // The image transforms transform the images into the model's expected format.
                .Append(mLContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                .Append(mLContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                .Append(mLContext.Model.LoadTensorFlowModel(_inceptionTensorFlowModel)
                    .ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" },
                                          inputColumnNames: new[] { "input" },
                                          addBatchDimensionInput: true))
                .Append(mLContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                .Append(mLContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                .Append(mLContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                .AppendCacheCheckpoint(mLContext);

            IDataView trainingData = mLContext.Data.LoadFromTextFile<ImageData>(path: _trainTagsTsv, hasHeader: false);

            ITransformer model = pipeline.Fit(trainingData);

            IDataView testData = mLContext.Data.LoadFromTextFile<ImageData>(path: _testTagsTsv, hasHeader: false);
            IDataView predictions = model.Transform(testData);

            // Create an IEnumerable for the predictions for displaying results
            IEnumerable<ImagePrediction> imagePredictionData = mLContext.Data.CreateEnumerable<ImagePrediction>(predictions, true);
            DisplayResults(imagePredictionData);

            MulticlassClassificationMetrics metrics =
                mLContext.MulticlassClassification.Evaluate(predictions,
                labelColumnName: "LabelKey",
                predictedLabelColumnName: "PredictedLabel");

            Console.WriteLine($"LogLoss is: {metrics.LogLoss}");
            Console.WriteLine($"PerClassLogLoss is: {String.Join(" , ", metrics.PerClassLogLoss.Select(c => c.ToString()))}");

            return (model, trainingData);
        }
    }
}
