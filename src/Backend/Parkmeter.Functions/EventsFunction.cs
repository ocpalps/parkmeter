// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Parkmeter.Core.Models;

namespace Parkmeter.Functions
{
    public static class EventsFunction
    {
        [FunctionName("ErrorEvent")]
        public static async Task ErrorEvent([EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
           
            log.LogInformation("Error Event: " + eventGridEvent.Data.ToString());

            //TODO: do something useful
        }

        [FunctionName("TrafficWorker")]
        public static async Task TrafficWorker([ServiceBusTrigger(queueName:"trafficqueue", Connection = "ServiceBusConnection")] VehicleAccess vehicleAccess,
            [CosmosDB(ConnectionStringSetting = "CosmosDBEndpoint")] DocumentClient client,
            ILogger log)
        {
            try
            {
                var va = new VehicleAccessDocument()
                {
                    Access = vehicleAccess
                };

                await FunctionsHelper.SaveEntryAsync(va, "VehicleAccesses", client, log);
                log.LogInformation($"Entry saved succesfully: {vehicleAccess.VehicleID}");

                var succeded = new EventGridHelper.ParkmeterEvent()
                {
                    Message = $"Vehicle: {vehicleAccess.VehicleID} - Direction: {vehicleAccess.Direction.ToString()}",
                    Type = EventGridHelper.EventType.Succeded
                };
                EventGridHelper.SendEvent(succeded);
                log.LogInformation($"Succeded event sent");

            }
            catch (Exception ex)
            {
                var failure = new EventGridHelper.ParkmeterEvent()
                {
                    Message = "Save to db failed",
                    Type = EventGridHelper.EventType.Error
                };
                EventGridHelper.SendEvent(failure);
                log.LogInformation($"Error event sent");
            }

        }

        [FunctionName("FileUploaded")]
        public static async Task FileUploaded([BlobTrigger("upload/{name}", Connection = "ImageStorage")]Stream myBlob, string name, Uri uri, ExecutionContext context,
            [ServiceBus("trafficqueue", Connection = "ServiceBusConnection", EntityType = EntityType.Queue)] IAsyncCollector<VehicleAccess> vehicleAccessQueue,
            ILogger log)
        {
            log.LogInformation($"File uploaded: {name} \n Size: {myBlob.Length} Bytes");

            var config = Tools.InitConfig(context);

            //send image for OCR recognition
            ComputerVisionClient computerVision = new ComputerVisionClient(
               new ApiKeyServiceClientCredentials(config["ComputerVisionKey"]),
               new System.Net.Http.DelegatingHandler[] { });

            computerVision.Endpoint = config["ComputerVisionEndpoint"];

            var result = await ExtractRemoteTextAsync(computerVision, uri.AbsoluteUri);

            if (result.Status != TextOperationStatusCodes.Succeeded)
            { 
                var failure = new EventGridHelper.ParkmeterEvent()
                {
                    Message = "No text found on image",
                    Type = EventGridHelper.EventType.Error
                };
                EventGridHelper.SendEvent(failure);
                log.LogInformation($"Error event sent");
            }

            bool found = false;
            string pattern = @"^(([a-zA-Z]{2}\d{3}[a-zA-Z]{2})|(([a-zA-Z]{2}|roma)(\d{5}|\d{6})))$";
            foreach (string text in result.RecognitionResult.Lines.Select(t => t.Text))
            {
                string plate = text.Trim().Replace(" ", "");
                var matched = System.Text.RegularExpressions.Regex.Match(plate, pattern);
                if (matched.Success)
                {
<<<<<<< HEAD
                    //Enqueue message to service bus queue
                    vehicleAccessQueue.AddAsync(new VehicleAccess()
                    {
                        Direction = AccessDirections.In,
                        ParkingID = 1,
                        SpaceID = 2,
                        VehicleID = plate,
                        VehicleType = VehicleTypes.Car
                    }));
=======
                    var succeded = new EventGridHelper.ParkmeterEvent()
                    {
                        Message = "License plate recognized",
                        Type = EventGridHelper.EventType.Succeded,
                        Data = JsonConvert.SerializeObject(new VehicleAccess() { Direction = AccessDirections.In, ParkingID = 1, SpaceID = 2, VehicleID = plate, VehicleType = VehicleTypes.Car })
                    };
>>>>>>> dc6ce02a12c46cd9fce4d3ddede52bf7ce732108

                    log.LogInformation(succeded.Message);

                    found = true;

                    break;
                }
            }

            if(!found)
            {
                var failure = new EventGridHelper.ParkmeterEvent()
                {
                    Message = "License plate format is not valid",
                    Type = EventGridHelper.EventType.Error
                };
                EventGridHelper.SendEvent(failure);
            }
        }

        // Recognize text from a remote image
        private async static Task<TextOperationResult> ExtractRemoteTextAsync(ComputerVisionClient computerVision, string imageUrl)
        {
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                Debug.WriteLine("\nInvalid remoteImageUrl:\n{0} \n", imageUrl);
                return null;
            }

            // Start the async process to recognize the text
            RecognizeTextHeaders textHeaders = await computerVision.RecognizeTextAsync(imageUrl, TextRecognitionMode.Printed);

            // Retrieve the URI where the recognized text will be
            // stored from the Operation-Location header
            string operationId = textHeaders.OperationLocation.Substring(textHeaders.OperationLocation.Length - 36);

            TextOperationResult result = await computerVision.GetTextOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running ||
                    result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                Debug.WriteLine("Server status: {0}, waiting {1} seconds...", result.Status, i);
                await Task.Delay(1000);

                result = await computerVision.GetTextOperationResultAsync(operationId);
            }

            // Return the results
            return result;
        }

    }
}
