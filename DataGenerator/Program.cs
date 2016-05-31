using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using GalaxyService.Shared.Models;

namespace DataGenerator
{
    public class Program
    {
        /// <summary>
        /// The Azure DocumentDB endpoint
        /// </summary>
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["ServiceEndPointUri"];

        private readonly StarGenerator _starGenerator = new StarGenerator(5, 50);
        private const int NumberOfGalaxies = 50;

        HttpClient _httpClient = new HttpClient();
        

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.GenerateDataAndSave().Wait();
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