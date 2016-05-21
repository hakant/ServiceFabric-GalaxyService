using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GalaxyService.Shared.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace DataGenerator
{
    public class Program
    {
        /// <summary>
        /// The Azure DocumentDB endpoint
        /// </summary>
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"];

        /// <summary>
        /// The primary key for the Azure DocumentDB account.
        /// </summary>
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        /// <summary>
        /// The DocumentDB client instance.
        /// </summary>
        private DocumentClient _client;

        private readonly StarGenerator _starGenerator = new StarGenerator(5, 50);
        private const int NumberOfGalaxies = 50;

        const string GalaxyDbName = "GalaxyDb";
        const string StarCollectionName = "StarCollection";
        

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.GenerateDataAndSave().Wait();
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
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
            // Create a new instance of the DocumentClient
            _client = new DocumentClient(new Uri(EndpointUri), PrimaryKey);

            await CreateDatabaseIfNotExists(GalaxyDbName);
            await DeleteDocumentCollectionIfExists(GalaxyDbName, StarCollectionName);
            await CreateDocumentCollectionIfNotExists(GalaxyDbName, StarCollectionName);

            WriteToConsole("Generating stars...");
            var stars = _starGenerator.GenerateRandomStars(NumberOfGalaxies).ToList();
            var totalCount = stars.Count;
            var counter = 0;

            WriteToConsoleAndPromptRead($"{totalCount} stars have been generated. Star writing to the database?");

            foreach (var star in stars)
            {
                counter++;
                await CreateDocumentIfNotExists(GalaxyDbName, StarCollectionName, star);

                WriteToConsole($"{counter} out of {totalCount} is completed.");
            }
        }

        /// <summary>
        /// Create a database with the specified name if it doesn't exist. 
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <returns>The Task for asynchronous execution.</returns>
        private async Task CreateDatabaseIfNotExists(string databaseName)
        {
            // Check to verify a database with the id={databaseName} does not exist
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
                WriteToConsole("Found {0}", databaseName);
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist, create a new database
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = databaseName });
                    WriteToConsole("Created {0}", databaseName);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Create a collection with the specified name if it doesn't exist.
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <param name="collectionName">The name/ID of the collection.</param>
        /// <returns>The Task for asynchronous execution.</returns>
        private async Task CreateDocumentCollectionIfNotExists(string databaseName, string collectionName)
        {
            try
            {
                await this._client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
                WriteToConsole("Found {0}", collectionName);
            }
            catch (DocumentClientException de)
            {
                // If the document collection does not exist, create a new collection
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    DocumentCollection collectionInfo = new DocumentCollection();
                    collectionInfo.Id = collectionName;

                    // Optionally, you can configure the indexing policy of a collection. Here we configure collections for maximum query flexibility 
                    // including string range queries. 
                    collectionInfo.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });

                    // DocumentDB collections can be reserved with throughput specified in request units/second. 1 RU is a normalized request equivalent to the read
                    // of a 1KB document.  Here we create a collection with 400 RU/s. 
                    await this._client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseName),
                        new DocumentCollection { Id = collectionName },
                        new RequestOptions { OfferThroughput = 400 });

                    WriteToConsole("Created {0}", collectionName);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Delete a collection with the specified name if it exists.
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <param name="collectionName">The name/ID of the collection.</param>
        /// <returns>The Task for asynchronous execution.</returns>
        private async Task DeleteDocumentCollectionIfExists(string databaseName, string collectionName)
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
                WriteToConsole("Deleting collection with name {0}", collectionName);

                await _client.DeleteDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName)
                    );

                WriteToConsole("{0} collection deleted.", collectionName);
            }
            catch (DocumentClientException de)
            {
                // If the document collection does not exist, there's nothing to do
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    WriteToConsole("{0} collection doesn't exist.", collectionName);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Create the document in the collection if another by the same ID doesn't already exist.
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <param name="collectionName">The name/ID of the collection.</param>
        /// <param name="star">The star document to be created.</param>
        /// <returns>The Task for asynchronous execution.</returns>
        private async Task CreateDocumentIfNotExists(string databaseName, string collectionName, StarEntity star)
        {
            try
            {
                await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, star.Id));
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), star);
                }
                else
                {
                    throw;
                }
            }
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