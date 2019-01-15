using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Parkmeter.ParkingDataConsole
{
    public class Program
    {
        private const string EndpointUrl = "https://localhost:8081";
        private const string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private DocumentClient client;

        public static void Main(string[] args)
        {

            try
            {
                Program p = new Program();
                p.LoadTestData().Wait();
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
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private async Task LoadTestData()
        {
            ConnectionPolicy connectionPolicy = new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey, connectionPolicy);



            string dbName = "ParkingLedger";
            string collectionName = "VehicleAccesses";
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = dbName });
            var collection = await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(dbName), new DocumentCollection { Id = collectionName });

            // manual update
            //var list = CreateAccessesList();
            //Parallel.ForEach(list, (x) =>
            //{
            //    RegisterVehicleAccess(dbName, collectionName, x);
            //});


            // Set retry options high during initialization (default values).
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;

            IBulkExecutor bulkExecutor = new BulkExecutor(client, collection);
            await bulkExecutor.InitializeAsync();

            // Set retries to 0 to pass complete control to bulk executor.
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

            var list = CreateAccessesList();
            var listOfStrings = list.Select(item => JsonConvert.SerializeObject(item)).ToList();
            var documents = JsonConvert.SerializeObject(list);

            BulkImportResponse bulkImportResponse = await bulkExecutor.BulkImportAsync(
              documents: listOfStrings,
              enableUpsert: true,
              disableAutomaticIdGeneration: true,
              maxConcurrencyPerPartitionKeyRange: null,
              maxInMemorySortingBatchSize: null);

            Console.WriteLine("Bulk import completed:");
            Console.WriteLine($"\tImported: { bulkImportResponse.NumberOfDocumentsImported}");
            Console.WriteLine($"\tErrors: { bulkImportResponse.BadInputDocuments.Count}");
            Console.WriteLine($"\tRequestUnits: { bulkImportResponse.TotalRequestUnitsConsumed}");
            Console.WriteLine($"\tTime taken: { bulkImportResponse.TotalTimeTaken}");

        }

        private List<VehicleAccessDocument> CreateAccessesList()
        {
            List<VehicleAccessDocument> list = new List<VehicleAccessDocument>();

            for (int month = 1; month <= 12; month++)
            {
                for (int day = 1; day <= 28; day++)
                {
                    Random parkingHours = new Random(DateTime.Now.TimeOfDay.Seconds);

                    DateTime parkingDay = new DateTime(2018, month, day, parkingHours.Next(8), parkingHours.Next(50), 00);

                    // less parking on weekends
                    int numberOfVehicles = 100;
                    if (parkingDay.DayOfWeek == DayOfWeek.Sunday || parkingDay.DayOfWeek == DayOfWeek.Saturday)
                        numberOfVehicles = 30;

                    for (; numberOfVehicles > 0; numberOfVehicles--)
                    {
                        string vehicleId = $"BD{day}{day}AS{numberOfVehicles}";
                        VehicleAccess @in = new VehicleAccess
                        {
                            Direction = AccessDirections.In,
                            ParkingID = 1,
                            SpaceID = day,
                            VehicleID = vehicleId,
                            TimeStamp = parkingDay,
                            VehicleType = VehicleTypes.Car
                        };
                        VehicleAccess @out = new VehicleAccess
                        {
                            Direction = AccessDirections.Out,
                            ParkingID = 1,
                            SpaceID = day,
                            VehicleID = vehicleId,
                            TimeStamp = parkingDay.AddHours(parkingHours.Next(8)), //max 8 hours of parking
                            VehicleType = VehicleTypes.Car
                        };
                        list.Add(new VehicleAccessDocument() { Access = @in, id = Guid.NewGuid().ToString() });
                        list.Add(new VehicleAccessDocument() { Access = @out, id = Guid.NewGuid().ToString() });
                    }


                }
            }

            Console.WriteLine($"Created a list with {list.Count} vehicle accesses");

            return list;
        }

        // for manual import
        private async Task RegisterVehicleAccess(string databaseName, string collectionName, VehicleAccess access)
        {
            try
            {
                var doc = await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), access);
                Console.WriteLine($"Added new access: {doc.Resource.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to register a new vechicle access: " + ex.Message);
            }
        }


    }
}
