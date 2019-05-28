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
            [CosmosDB(ConnectionStringSetting = "CosmosDBEndpoint")] DocumentClient client, 
            ILogger log)
        {
            var error = new
            {
                Message = eventGridEvent.Data.ToString()
            };

            await FunctionsHelper.SaveEntryAsync(error, "Errors", client, log);

            log.LogInformation("Error Event: " + eventGridEvent.Data.ToString());
        }

        [FunctionName("SuccededEvent")]
        public static async Task SuccededEvent([EventGridTrigger]EventGridHelper.ParkmeterEvent eventGridEvent,
            [CosmosDB(ConnectionStringSetting = "CosmosDBEndpoint")] DocumentClient client,
            ILogger log)
        {
            var va = new VehicleAccessDocument() { 
                Access = JsonConvert.DeserializeObject<VehicleAccess>(eventGridEvent.Data)
            };


            await FunctionsHelper.SaveEntryAsync(va, "VehicleAccesses", client, log);
            log.LogInformation("Success Event: " + eventGridEvent.Data.ToString());
        }

        [FunctionName("FileUploaded")]
        public static async Task FileUploaded([BlobTrigger("upload/{name}", Connection = "ImageStorage")]Stream myBlob, string name, Uri uri, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"File uploaded: {name} \n Size: {myBlob.Length} Bytes");

            var config = Tools.InitConfig(context);

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
                    var succeded = new EventGridHelper.ParkmeterEvent()
                    {
                        Message = "License plate recognized",
                        Type = EventGridHelper.EventType.Succeded,
                        Data = JsonConvert.SerializeObject(new VehicleAccess() { Direction = AccessDirections.In, ParkingID = 1, SpaceID = 2, VehicleID = plate, VehicleType = VehicleTypes.Car })
                    };

                    log.LogInformation(succeded.Message);

                    EventGridHelper.SendEvent(succeded);
                    found = true;

                    log.LogInformation($"Succeded event sent");

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
