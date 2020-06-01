using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace projekt_grupowy
{
    class ML
    {
        private List<SingleRow> dataToLearn, dataToTest;
        string modelName = "trackingModel.zip";
        string ModelPath;
        int accuratePrediction;
        int oneDayPrediction;
        float percentage;
        float percentage2;
        MLContext mlContext;


        public ML()
        {
            percentage = 0;
            percentage2 = 0;
            accuratePrediction = 0;
            oneDayPrediction = 0;
            ModelPath = Path.Combine(Environment.CurrentDirectory, @"Models\", modelName);
            mlContext = new MLContext(seed: 0);

        }

        public void loadDataToTest()
        {
            dataToTest = readCsvFile(@"E:\Raport\Marzec_2020.csv", 1);
        }

        public void loadDataToLearn()
        {
            dataToLearn = readCsvFile(@"E:\Raport\tracking_data.csv", 0);
        }

        public void train()
        {
            Train(mlContext);
        }

        public void TestMultiplePredictions()
        {
            TestMultiplePredictions(mlContext, @"E:\Raport\testData.csv");
        }

        private void convertFileToCsv(String inputFilePath, string outputFilePath) //czas trwania około 3 minuty
        {
            StreamWriter outputWriter = File.CreateText(outputFilePath);
            StreamReader inputReader = File.OpenText(inputFilePath);
            string firstLine;
            firstLine = inputReader.ReadLine();
            string sentence = "SHIPMENT_IDENTCODE,SHIPMENT_CREATEDATE,SHIPMENT_CREATETIME," +
                                "FIRST_EVENT,FIRST_EVENT_TIME,LAST_EVENT,LAST_EVENT_TIME,RECEIVER_ZIP," +
                                "RECEIVER_COUNTRY_IOS2,SENDER_ZIP,SENDER_COUNTRY_IOS2,SHIPMENT_WEIGHT," +
                                "CONTRACT_TYPE,XLIDENTIFIER";
            outputWriter.WriteLine(sentence);
            firstLine = inputReader.ReadLine();

            while (firstLine != "")
            {
                firstLine = inputReader.ReadLine();
                if (firstLine.Contains("NULL"))
                {
                    continue;
                }
                sentence = Regex.Replace(firstLine, @"\s+", ",");
                outputWriter.WriteLine(sentence);
            }
            outputWriter.Close();
        }

        private static List<SingleRow> readCsvFile(string csvFilePath, int check)
        {
            List<SingleRow> dataInList = new List<SingleRow>();
            Console.WriteLine("Reading .csv file");
            int i = 0, j = 0, k = 0;
            StreamReader reader = new StreamReader(File.OpenRead(csvFilePath));
            reader.ReadLine();
            string line = reader.ReadLine();

            if (check == 0) k = 14000000;
            else if (check == 1) k = 5800000;

            while (i < k)
            {
                line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine("Read line no." + i);
                    i++;
                    string[] values = line.Split(',');

                    if (values[8].Equals("PL") && (values[10].Equals("PL")))
                    {
                        SingleRow newRow = new SingleRow(
                                    values[4], values[3], values[7], 
                                    values[9], values[6], values[5]
                                );
                        if (newRow.distance != 0)
                        {
                            j++;
                            dataInList.Add(newRow);
                            Console.WriteLine("Added " + j + " items");
                        }
                    }  
                }
            }
            return dataInList;
        }

        private void Train(MLContext mlContext)
        {
            IDataView trackingData = mlContext.Data.LoadFromEnumerable<SingleRow>(dataToLearn);
            IDataView filteredData = mlContext.Data.FilterRowsByColumn(trackingData, "distance", lowerBound: 1, upperBound: 850);
            var dataProcessPipeline = mlContext.Transforms.Concatenate("Features", nameof(SingleRow.distance))
                            /*.Append(mlContext.Transforms.NormalizeMeanVariance("Features"))*/;

            //var trainer = mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features");
            var trainer = mlContext.Regression.Trainers.LightGbm(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);
            Console.WriteLine("=============== Training the model ===============");
            var trainedModel = trainingPipeline.Fit(filteredData);
            
            mlContext.Model.Save(trainedModel, trackingData.Schema, ModelPath);

            Console.WriteLine("The model is saved to {0}", ModelPath);        
        }

        public DateTime TestSinglePrediction(SingleRow rowToPredict)
        {

            ITransformer trainedModel = mlContext.Model.Load(ModelPath, out var modelInputSchema);

            var predEngine = mlContext.Model.CreatePredictionEngine<SingleRow, DatePrediction>(trainedModel);

            var resultprediction = predEngine.Predict(rowToPredict);


            return resultprediction.GivePredictedDate(rowToPredict.first_event);
        }

        private void TestMultiplePredictions(MLContext mlContext, string OutputFilePath)
        {
            ITransformer trainedModel = mlContext.Model.Load(ModelPath, out var modelInputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<SingleRow, DatePrediction>(trainedModel);
            StreamWriter outputWriter = File.CreateText(OutputFilePath);
            DateTime predicted;
            DateTime actual;
            string sentence = "Predicted_date,Actual_date";
            outputWriter.WriteLine(sentence);

            for (int i = 0; i < dataToTest.Count; i++)
            {
                var resultprediction = predEngine.Predict(dataToTest[i]);
                predicted = resultprediction.GivePredictedDate(dataToTest[i].first_event);
                actual = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(dataToTest[i].last_event)).DateTime.ToLocalTime();
                if (predicted.DayOfYear == actual.DayOfYear)
                {
                    accuratePrediction = accuratePrediction + 1;
                }
                if((predicted.DayOfYear - actual.DayOfYear) == 1 || (predicted.DayOfYear - actual.DayOfYear) == -1)
                {
                    oneDayPrediction = oneDayPrediction + 1;
                }
                Console.WriteLine($"**********************************************************************");
                Console.WriteLine($"Predicted arrival date: {predicted.ToString()}");
                Console.WriteLine($"Actual arrival date: {actual}");
                sentence = predicted.ToString() + "," + actual;
                outputWriter.WriteLine(sentence);
            }
            outputWriter.Close();
            percentage = (accuratePrediction / dataToTest.Count) * 100;
            Console.WriteLine("Accuracy: " + percentage + "%");
            percentage2 = (oneDayPrediction / dataToTest.Count) * 100;
            Console.WriteLine("Accuracy with one day error: " + percentage2 + "%");
            Console.ReadLine();
        }

    }
}