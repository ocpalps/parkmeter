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
            SqlQuery = "SELECT * FROM c WHERE c.ParkingID = {parkingId}", CreateIfNotExists = true, PartitionKey ="ParkingID")] IEnumerable<dynamic> docs,
            int parkingId,
            ILogger log)
        {
            if (docs == null || docs.Count() == 0)
                return new NotFoundResult();
            var doc = docs.FirstOrDefault();

            ParkingStatus status = new ParkingStatus
            {
                BusySpaces = (int)doc.busySpaces,
                ParkingId = doc.ParkingID
            };

            return new OkObjectResult(status);
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

            var va = new VehicleAccessDocument() { Access = JsonConvert.DeserializeObject<VehicleAccess>(data) };
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


        [FunctionName("CosmosDB-RegisterAccessStatusTrigger")]
        public static async Task Run([CosmosDBTrigger(databaseName: "ParkingLedger", collectionName: "VehicleAccesses", ConnectionStringSetting = "CosmosDBEndpoint",
            LeaseCollectionName = "leases", CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> documents,
            [CosmosDB(ConnectionStringSetting = "CosmosDBEndpoint")] DocumentClient client,
            ILogger log)
        {
            if (documents != null && documents.Count > 0)
            {
                if (JsonConvert.DeserializeObject<ParkingStatusDocument>(documents[0].ToString()).isStatus == true)
                    return;
                
                var accessDocument = JsonConvert.DeserializeObject<VehicleAccessDocument>( documents[0].ToString());

                var query = client.CreateDocumentQuery<ParkingStatusDocument>(UriFactory.CreateDocumentCollectionUri("ParkingLedger", "VehicleAccesses"))
                    .Where(ps=>ps.id == $"_status_{accessDocument.Access.ParkingID}");

                ParkingStatusDocument psd = null;
                if (query.Count() == 0)
                {
                    psd = new ParkingStatusDocument()
                    {
                        id = $"_status_{accessDocument.Access.ParkingID}",
                        ParkingID = accessDocument.Access.ParkingID,
                        isStatus = true,
                        busySpaces = 0
                    };
                }
                else
                {
                    psd = query.AsEnumerable().FirstOrDefault();
                }

                if (psd != null)
                {
                    psd.busySpaces += (int)accessDocument.Access.Direction;
                    var doc = await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("ParkingLedger", "VehicleAccesses"), psd);
                }
                
            }
        }
    }
}
