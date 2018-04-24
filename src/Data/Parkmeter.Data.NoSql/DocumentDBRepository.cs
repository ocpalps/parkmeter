using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Parkmeter.Core.Interfaces;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;
using System.IO;
using Parkmeter.Core.Models;
using System.Reflection;
using System.Diagnostics;

namespace Parkmeter.Data.NoSql
{
    public static class DocumentDBRepository<T> where T : class
    {

        private static Uri _endpoint = new Uri("https://localhost:8081/");
        private static string _key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="; //default localhost key
        private static readonly string _databaseId = "ParkingLedger";
        private static readonly string _collectionId = "VehicleAccesses";
        private static DocumentClient _client;        

        public static async Task<T> GetItemAsync(string id)
        {
            try
            {
                Document document = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = _client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            try
            {
                return await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), item, new RequestOptions { PostTriggerInclude = new List<string> { "UpdateParkingStatus" } });
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<Document> ExectureStoredProcedure(string storedProcedure, params dynamic[] parameters)
        {
            try
            {
                var returnVal = await _client.ExecuteStoredProcedureAsync<string>($"/{UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId).ToString()}/sprocs/{storedProcedure}", parameters);
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }

        public static bool Initialize(Uri endpoint, string key)
        {
            try
            {
                if (endpoint == null || String.IsNullOrEmpty(key))
                    throw new InvalidDataException("DocumentDB settings are invalid");

                _endpoint = endpoint;
                _key = key;
                _client = new DocumentClient(_endpoint, _key, new ConnectionPolicy { EnableEndpointDiscovery = false });
                CreateDatabaseIfNotExistsAsync().Wait();
                CreateCollectionIfNotExistsAsync().Wait();
                return true;
            }
            catch (Exception ex)
            {
                Debug.Write("DocumentDB init failed:" + ex.Message);
                return false;
            }
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = _databaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_databaseId),
                        new DocumentCollection { Id = _collectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<PersistenceResult> CreateTriggerAsync(string sourceFilePath, string triggerId)
        {
            try
            {
                string triggerBody;
                var assembly = typeof(DocumentDBRepository<T>).GetTypeInfo().Assembly;

                using (var stream = assembly.GetManifestResourceStream(sourceFilePath))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        triggerBody = reader.ReadToEnd();
                    }
                }
                
                Trigger trigger = new Trigger
                {
                    Id = Path.GetFileName(triggerId),
                    Body = triggerBody,
                    TriggerOperation = TriggerOperation.Create,
                    TriggerType = TriggerType.Post
                };
                var collection = await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId));
                await TryDeleteTrigger(collection.Resource.SelfLink, trigger.Id);
                await _client.CreateTriggerAsync(collection.Resource.SelfLink, trigger);
            }
            catch (Exception ex)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }

            return new PersistenceResult() { State = ResultStates.Completed };

        }

        /// <summary>
        /// If a Trigger is found on the DocumentCollection for the Id supplied it is deleted
        /// </summary>
        /// <param name="colSelfLink">DocumentCollection to search for the Trigger</param>
        /// <param name="triggerId">Id of the Trigger to delete</param>
        /// <returns></returns>
        private static async Task TryDeleteTrigger(string colSelfLink, string triggerId)
        {
            Trigger trigger = _client.CreateTriggerQuery(colSelfLink).Where(t => t.Id == triggerId).AsEnumerable().FirstOrDefault();

            if (trigger != null)
            {
                await _client.DeleteTriggerAsync(trigger.SelfLink);
            }
        }
    }
}
