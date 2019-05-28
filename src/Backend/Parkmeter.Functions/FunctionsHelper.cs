using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Parkmeter.Core.Models;

namespace Parkmeter.Functions
{
    public static class FunctionsHelper
    {

        public static async Task<IActionResult> SaveEntryAsync(object document, string collectionId, DocumentClient client, ILogger log)
        {

            if (document != null)
            {
                var db = new Database();
                db.Id = "ParkingLedger";
                var database = await client.CreateDatabaseIfNotExistsAsync(db);
                var collection = await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(db.Id), new DocumentCollection() { Id = collectionId });

                var doc = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("ParkingLedger", collectionId), document);
                return new OkResult();
            }

            return new BadRequestResult();
        }
    }


}
