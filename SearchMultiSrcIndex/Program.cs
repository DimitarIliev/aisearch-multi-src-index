using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using SearchMultiSrcIndex.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureSearch.SDKHowTo
{
    public sealed class Program
    {
        static IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        static IConfigurationRoot configuration = builder.Build();

        static async Task Main(string[] args)
        {
            var runMultiSource = false;

            var searchServiceUri = configuration["SearchServiceUri"];
            var adminApiKey = configuration["SearchServiceAdminApiKey"];

            var indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(adminApiKey));
            var indexerClient = new SearchIndexerClient(new Uri(searchServiceUri), new AzureKeyCredential(adminApiKey));

            if (runMultiSource)
            {
                await MultiSourceIndex(indexClient, indexerClient);
            }
            else
            {
                await SingleSourceIndex(indexClient, indexerClient);
            }
            


        }

        private static async Task SingleSourceIndex(SearchIndexClient indexClient, SearchIndexerClient indexerClient)
        {
            // use DataChangeDetectionPolicy and dataDeletionDetectionPolicy

            var indexName = "single-product-index";

            await CreateSingleIndexAsync(indexName, indexClient);

            await CreateAndRunCosmosDbSingleIndexerAsync(indexName, indexerClient);

            Console.WriteLine("Index created.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static async Task MultiSourceIndex(SearchIndexClient indexClient, SearchIndexerClient indexerClient)
        {
            var indexName = "products-index";

            await CreateIndexAsync(indexName, indexClient);

            await CreateAndRunCosmosDbSingleIndexerAsync(indexName, indexerClient);

            await CreateAndRunBlobIndexerAsync(indexName, indexerClient);

            Console.WriteLine("Index created.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static async Task CreateSingleIndexAsync(string indexName, SearchIndexClient indexClient)
        {
            var bulder = new FieldBuilder();
            var definition = new SearchIndex(indexName, bulder.Build(typeof(SingleProduct)));

            await indexClient.CreateIndexAsync(definition);
        }

        private static async Task CreateIndexAsync(string indexName, SearchIndexClient indexClient)
        {
            var bulder = new FieldBuilder();
            var definition = new SearchIndex(indexName, bulder.Build(typeof(Product)));

            await indexClient.CreateIndexAsync(definition);
        }

        private static async Task CreateAndRunCosmosDbSingleIndexerAsync(string indexName, SearchIndexerClient indexerClient)
        {
            var cosmosConnectString = $"{configuration["CosmosDBConnectionString"]};Database={configuration["CosmosDBDatabaseName"]}";

            var cosmosDbDataSource = new SearchIndexerDataSourceConnection(
                name: configuration["CosmosDBDatabaseName"],
                type: SearchIndexerDataSourceType.CosmosDb,
                connectionString: cosmosConnectString,
                container: new SearchIndexerDataContainer("Products"));

            cosmosDbDataSource.DataChangeDetectionPolicy = new HighWaterMarkChangeDetectionPolicy("_ts");

            cosmosDbDataSource.DataDeletionDetectionPolicy = new SoftDeleteColumnDeletionDetectionPolicy
            {
                SoftDeleteColumnName = "isDeleted",
                SoftDeleteMarkerValue = "true"
            };

            await indexerClient.CreateOrUpdateDataSourceConnectionAsync(cosmosDbDataSource);

            Console.WriteLine("Creating Cosmos DB indexer...\n");

            var cosmosDbIndexer = new SearchIndexer(
                name: "single-product-indexer",
                dataSourceName: cosmosDbDataSource.Name,
                targetIndexName: indexName)
            {
                Schedule = new IndexingSchedule(TimeSpan.FromDays(1))
            };

            try
            {
                await indexerClient.GetIndexerAsync(cosmosDbIndexer.Name);

                await indexerClient.ResetIndexerAsync(cosmosDbIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { }

            await indexerClient.CreateOrUpdateIndexerAsync(cosmosDbIndexer);

            Console.WriteLine("Running Cosmos DB indexer...\n");

            try
            {
                await indexerClient.RunIndexerAsync(cosmosDbIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                Console.WriteLine("Failed to run indexer: {0}", ex.Message);
            }
        }

        private static async Task CreateAndRunCosmosDbIndexerAsync(string indexName, SearchIndexerClient indexerClient)
        {
            var cosmosConnectString = $"{configuration["CosmosDBConnectionString"]};Database={configuration["CosmosDBDatabaseName"]}";

            var cosmosDbDataSource = new SearchIndexerDataSourceConnection(
                name: configuration["CosmosDBDatabaseName"],
                type: SearchIndexerDataSourceType.CosmosDb,
                connectionString: cosmosConnectString,
                container: new SearchIndexerDataContainer("Products"));

            await indexerClient.CreateOrUpdateDataSourceConnectionAsync(cosmosDbDataSource);

            Console.WriteLine("Creating Cosmos DB indexer...\n");

            var cosmosDbIndexer = new SearchIndexer(
                name: "products-indexer",
                dataSourceName: cosmosDbDataSource.Name,
                targetIndexName: indexName)
            {
                Schedule = new IndexingSchedule(TimeSpan.FromDays(1))
            };

            try
            {
                await indexerClient.GetIndexerAsync(cosmosDbIndexer.Name);

                await indexerClient.ResetIndexerAsync(cosmosDbIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { }

            await indexerClient.CreateOrUpdateIndexerAsync(cosmosDbIndexer);

            Console.WriteLine("Running Cosmos DB indexer...\n");

            try
            {
                await indexerClient.RunIndexerAsync(cosmosDbIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                Console.WriteLine("Failed to run indexer: {0}", ex.Message);
            }
        }

        private static async Task CreateAndRunBlobIndexerAsync(string indexName, SearchIndexerClient indexerClient)
        {
            var blobDataSource = new SearchIndexerDataSourceConnection(
                name: configuration["BlobStorageAccountName"],
                type: SearchIndexerDataSourceType.AzureBlob,
                connectionString: configuration["BlobStorageConnectionString"],
                container: new SearchIndexerDataContainer("productdata"));

            await indexerClient.CreateOrUpdateDataSourceConnectionAsync(blobDataSource);

            var parameters = new IndexingParameters();
            parameters.Configuration.Add("parsingMode", "json");

            var blobIndexer = new SearchIndexer(
                name: "products-blob-indexer",
                dataSourceName: blobDataSource.Name,
                targetIndexName: indexName)
            {
                Parameters = parameters,
                Schedule = new IndexingSchedule(TimeSpan.FromDays(1))
            };

            try
            {
                await indexerClient.GetIndexerAsync(blobIndexer.Name);

                await indexerClient.ResetIndexerAsync(blobIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) { }

            await indexerClient.CreateOrUpdateIndexerAsync(blobIndexer);

            Console.WriteLine("Running Blob Storage indexer...\n");

            try
            {
                await indexerClient.RunIndexerAsync(blobIndexer.Name);
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                Console.WriteLine("Failed to run indexer: {0}", ex.Message);
            }
        }
    }
}