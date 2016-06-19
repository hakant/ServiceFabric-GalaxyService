using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using GalaxyService.Shared.Models;
using Newtonsoft.Json;

namespace DataGenerator
{
    public class Program
    {
        /// <summary>
        /// The Azure DocumentDB endpoint
        /// </summary>
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["ServiceEndPointUri"];

        private static readonly Dictionary<string, int> PartitionCounts = new Dictionary<string, int>();

        private readonly StarGenerator _starGenerator = new StarGenerator(50, 100);
        private const int NumberOfGalaxies = 250;

        readonly HttpClient _httpClient = new HttpClient();
        

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.GenerateDataAndSave().Wait();
                ReportPartitionCounts();
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ReportPartitionCounts()
        {
            WriteToConsole("Reporting Sharding Stats:");
            foreach (var partitionCount in PartitionCounts)
            {
                WriteToConsole($"{partitionCount.Key}: {partitionCount.Value}");
            }
        }

        private async Task GenerateDataAndSave()
        {
            WriteToConsole("Generating stars...");
            var stars = _starGenerator.GenerateRandomStars(NumberOfGalaxies).ToList();
            var totalCount = stars.Count;
            var counter = 0;

            WriteToConsoleAndPromptRead($"{totalCount} stars have been generated. Start pushing to the service?");

            foreach (var star in stars)
            {
                counter++;
                await PushToTheService(star);

                WriteToConsole($"{counter} out of {totalCount} is completed.");
            }
        }

        private async Task PushToTheService(StarEntity star)
        {
            var result = await _httpClient.PostAsJsonAsync(EndpointUri, star);
            var response = JsonConvert.DeserializeObject<StarInsertResult>(await result.Content.ReadAsStringAsync());

            CountPartition(response.ServicePartitionId);
        }

        private static void CountPartition(string partitionId)
        {
            if (PartitionCounts.ContainsKey(partitionId))
                PartitionCounts[partitionId] = PartitionCounts[partitionId] + 1;
            else
                PartitionCounts.Add(partitionId, 1);
        }
        
        /// <summary>
        /// Write to the console, and prompt to continue.
        /// </summary>
        /// <param name="format">The string to be displayed.</param>
        /// <param name="args">Optional arguments.</param>
        private static void WriteToConsole(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        private static void WriteToConsoleAndPromptRead(string format, params object[] args)
        {
            WriteToConsole(format, args);
            Console.WriteLine("Press any key to continue");
            Console.ReadLine();
        }

        internal class Galaxy
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}