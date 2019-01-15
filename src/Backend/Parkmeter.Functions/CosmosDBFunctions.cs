using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Parkmeter.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Parkmeter.Functions
{
    public static class CosmosDBFunctions
    {
        [FunctionName("CosmosDB-GetParkingStatus")]
        public static IActionResult GetParkingStatusAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getparkingstatus/{parkingId}")] HttpRequestMessage req,
            [CosmosDB("ParkingLedger", "VehicleAccesses", ConnectionStringSetting = "CosmosDBEndpoint", 
            SqlQuery = "SELECT * FROM c WHERE c.parkingID = {parkingId}", CreateIfNotExists = true, PartitionKey ="ParkingID")] IEnumerable<dynamic> docs,
            int parkingId,
            ILogger log)
        {
            if (docs == null || docs.Count() == 0)
                return new NotFoundResult();
            var doc = docs.FirstOrDefault();

            ParkingStatus status = new ParkingStatus
            {
                BusySpaces = doc.busySpaces,
                ParkingId = doc.parkingID
            };

            return new OkObjectResult(status);
        }

        [FunctionName("CosmosDB-GetLastVehicleAccess")]
        public static IActionResult GetLastVehicleAccess(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getlastvehicleaccess/{parkingId}/{vehicleId}")] HttpRequestMessage req,
            [CosmosDB("ParkingLedger", "VehicleAccesses", ConnectionStringSetting = "CosmosDBEndpoint",
            SqlQuery = "SELECT TOP 1 * FROM c WHERE c.ParkingID = {parkingId} AND c.VehicleID = {vehicleId} ORDER BY c.TimeStamp DESC", CreateIfNotExists = true, PartitionKey ="ParkingID")] IEnumerable<dynamic> docs,
            int parkingId,
            string vehicleId,
            ILogger log)
        {
            if (docs == null || docs.Count() == 0)
                return new NotFoundResult();
            var doc = docs.FirstOrDefault();

            VehicleAccess va = JsonConvert.DeserializeObject<VehicleAccess>(doc.ToString());

            return new OkObjectResult(va);
        }

        [FunctionName("CosmosDB-RegisterAccess")]
        public static async Task<IActionResult> RegisterAccessAsync(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registeraccess")] HttpRequestMessage req,
          [CosmosDB(ConnectionStringSetting = "CosmosDBEndpoint")] DocumentClient client,
          ILogger log)
        {
            // Get request body
            var data = req.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(data))
                return new BadRequestObjectResult("no payload");

            VehicleAccess va = JsonConvert.DeserializeObject<VehicleAccess>(data);
            if (va != null)
            {
                var db = new Database();
                db.Id = "ParkingLedger";
                var database = await client.CreateDatabaseIfNotExistsAsync(db);
                var collection = await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(db.Id), new DocumentCollection() { Id = "VehicleAccesses" });
                
                var doc = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("ParkingLedger", "VehicleAccesses"), va);
                return new OkResult();
            }

            return new BadRequestResult();
        }
    }
}
