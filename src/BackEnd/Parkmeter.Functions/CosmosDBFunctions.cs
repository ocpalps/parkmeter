using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Parkmeter.Core.Models;
using Microsoft.Azure.Documents.Client;
using System.Collections.Generic;
using System.Linq;

namespace Parkmeter.Functions
{
    public static class CosmosDBFunctions
    {
        [FunctionName("CosmosDB-GetParkingStatus")]
        public static async Task<IActionResult> GetParkingStatusAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getparkingstatus/{parkingId}")] HttpRequestMessage req,
            [CosmosDB("ParkingLedger", "VehicleAccesses", ConnectionStringSetting = "CosmosDBEndpoint", SqlQuery = "SELECT * FROM c WHERE c.parkingID = {parkingId}", CreateIfNotExists = true)] IEnumerable<dynamic> docs,
            int parkingId,
            ILogger log)
        {
            if (docs == null || docs.Count() == 0)
                return new NotFoundResult();
            var doc = docs.FirstOrDefault();
 

            ParkingStatus status = new ParkingStatus();
            status.BusySpaces = doc.busySpaces;
            status.ParkingId = doc.parkingID;

            return new OkObjectResult(status);
        }

       
        [FunctionName("CosmosDB-RegisterAccess")]
        public static IActionResult RegisterAccessAsync(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registeraccess")] HttpRequestMessage req,
          [CosmosDB("ParkingLedger", "VehicleAccesses", ConnectionStringSetting = "CosmosDBEndpoint", CreateIfNotExists = true)] DocumentClient client,
          ILogger log)
        {
        

            // Get request body
            var data = req.Content.ReadAsStringAsync().Result;
            if (String.IsNullOrEmpty(data))
                return new BadRequestObjectResult("no payload");

            VehicleAccess va = JsonConvert.DeserializeObject<VehicleAccess>(data);
            if (va != null)
            {
                client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("ParkingLedger", "VehicleAccesses"), va, new RequestOptions { PostTriggerInclude = new List<string> { "UpdateParkingStatus" } });
                return new OkResult();
            }

            return new BadRequestResult();
        }
    }
}
