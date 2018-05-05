﻿using ML = Microsoft.ML;
using Microsoft.ML;
using Microsoft.ML.Runtime;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Runtime.EntryPoints;
using System;
using System.IO;
using System.Threading.Tasks;

namespace eShopForecastModelsTrainer
{
    public class ProductModelHelper
    {
        /// <summary>
        /// Train and save model for predicting next month country unit sales
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <param name="outputModelPath">Trained model path</param>
        public static async Task SaveModel(string dataPath, string outputModelPath = "product_month_fastTreeTweedie.zip")
        {
            if (File.Exists(outputModelPath))
            {
                File.Delete(outputModelPath);
            }

            var model = CreateProductModelUsingPipeline(dataPath);

            await model.WriteAsync(outputModelPath);
        }


        /// <summary>
        /// Build model for predicting next month country unit sales using Learning Pipelines API
        /// </summary>
        /// <param name="dataPath">Input training file path</param>
        /// <returns></returns>
        private static PredictionModel<ProductData, ProductUnitPrediction> CreateProductModelUsingPipeline(string dataPath)
        {
            Console.WriteLine("*************************************************");
            Console.WriteLine("Training product forecasting model using Pipeline");

            var learningPipeline = new LearningPipeline();

            // First stage in the pipeline will be reading the source csv file
            learningPipeline.Add(new TextLoader<ProductData>(dataPath, header: true, sep: ","));

            // The model needs the columns to be arranged into a single column of numeric type
            // First, we group all numeric columns into a single array named NumericalFeatures
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(
                outputColumn: "NumericalFeatures",
                nameof(ProductData.year),
                nameof(ProductData.month),
                nameof(ProductData.max),
                nameof(ProductData.min),
                nameof(ProductData.idx),
                nameof(ProductData.count),
                nameof(ProductData.units),
                nameof(ProductData.avg),
                nameof(ProductData.prev)
            ));

            // Second group is for categorical features (just one in this case), we name this column CategoryFeatures
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(outputColumn: "CategoryFeatures", nameof(ProductData.productId)));

            // Then we need to transform the category column using one-hot encoding. This will return a numeric array
            learningPipeline.Add(new ML.Transforms.CategoricalOneHotVectorizer("CategoryFeatures"));

            // Once all columns are numeric types, all columns will be combined
            // into a single column, named Features 
            learningPipeline.Add(new ML.Transforms.ColumnConcatenator(outputColumn: "Features", "NumericalFeatures", "CategoryFeatures"));

            // Add the Learner to the pipeline. The Learner is the machine learning algorithm used to train a model
            // In this case, TweedieFastTree.TrainRegression was one of the best performing algorithms, but you can 
            // choose any other regression algorithm (StochasticDualCoordinateAscentRegressor,PoissonRegressor,...)
            learningPipeline.Add(new ML.Trainers.FastTreeTweedieRegressor { NumThreads = 1, FeatureColumn = "Features" });

            // Finally, we train the pipeline using the training dataset set at the first stage
            var model = learningPipeline.Train<ProductData, ProductUnitPrediction>();

            return model;
        }

        /// <summary>
        /// Predict samples using saved model
        /// </summary>
        /// <param name="outputModelPath">Model file path</param>
        /// <returns></returns>
        public static async Task TestPrediction(string outputModelPath = "product_month_fastTreeTweedie.zip")
        {
            Console.WriteLine("*********************************");
            Console.WriteLine("Testing product forecasting model");

            // Read the model that has been previously saved by the method SaveModel
            var model = await PredictionModel.ReadAsync<ProductData, ProductUnitPrediction>(outputModelPath);

            // Build sample data
            ProductData dataSample = new ProductData()
            {
                productId = "1527", month = 9, year = 2017, avg = 20, max = 41, min = 7,
                count = 30, prev = 559, units = 628, idx = 33 
            };

            // Predict sample data
            ProductUnitPrediction prediction = model.Predict(dataSample);
            Console.WriteLine($"Product: {dataSample.productId}, month: {dataSample.month+1}, year: {dataSample.year} - Real value (units): 778, Forecasting (units): {prediction.Score}");

            dataSample = new ProductData()
            {
                productId = "1527", month = 10, year = 2017, avg = 25, max = 41, min = 11,
                count = 31, prev = 628, units = 778, idx = 34
            };

            prediction = model.Predict(dataSample);
            Console.WriteLine($"Product: {dataSample.productId}, month: {dataSample.month+1}, year: {dataSample.year} - Forecasting (units): {prediction.Score}");

            dataSample = new ProductData()
            {
                productId = "1511", month = 9, year = 2017, avg = 2, max = 3, min = 1,
                count = 6, prev = 15, units = 13, idx = 33
            };

            prediction = model.Predict(dataSample);
            Console.WriteLine($"Product: {dataSample.productId}, month: {dataSample.month+1}, year: {dataSample.year} - Real Value (units): 12, Forecasting (units): {prediction.Score}");

            dataSample = new ProductData()
            {
                productId = "1511", month = 10, year = 2017, avg = 2, max = 5, min = 1,
                count = 6, prev = 13, units = 12, idx = 34
            };

            prediction = model.Predict(dataSample);
            Console.WriteLine($"Product: {dataSample.productId}, month: {dataSample.month+1}, year: {dataSample.year} - Forecasting (units): {prediction.Score}");
        }
    }
}
