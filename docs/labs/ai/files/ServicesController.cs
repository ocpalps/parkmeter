using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Parkmeter.Persistence;
using Swashbuckle.Swagger.Annotations;

namespace Parkmeter.Api.Controllers
{
    [Route("[controller]")]
    public class ServicesController : Controller
    {
        private PersistenceManager _store;
        private IConfiguration _configuration;

        private string _subscriptionKey;

        private const TextRecognitionMode textRecognitionMode = TextRecognitionMode.Printed;
        private const int numberOfCharsInOperationId = 36;

        public ServicesController(PersistenceManager store, IConfiguration configuration)
        {
            _configuration = configuration;
            _store = store;
            _subscriptionKey = _configuration.GetSection("CognitiveServices")["SubscriptionKey"];
        }

        [HttpGet("{parkingId}/{imageUrl}")]
        [SwaggerOperation(operationId: "CheckVehiclePresence")] //for autorest
        [Produces("application/json", Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CheckVehiclePresence(int parkingId, string imageUrl)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();

            var parking = _store.GetParking(parkingId);
            if (parking == null)
                return NotFound();

            ComputerVisionClient computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(_subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });

            computerVision.Endpoint = _configuration.GetSection("CognitiveServices")["Endpoint"];

            var result = await ExtractRemoteTextAsync(computerVision, Uri.UnescapeDataString(imageUrl));

            if (result.Status != TextOperationStatusCodes.Succeeded)
                return BadRequest();

            string pattern = @"^(([a-zA-Z]{2}\d{3}[a-zA-Z]{2})|(([a-zA-Z]{2}|roma)(\d{5}|\d{6})))$";
            foreach (string text in result.RecognitionResult.Lines.Select(t=>t.Text))
            {
                string plate = text.Trim().Replace(" ", "");
                var matched = System.Text.RegularExpressions.Regex.Match(plate, pattern);
                if (matched.Success)
                {
                    var lastAccess = await _store.GetLastVehicleAccess(parkingId, plate);
                    if (lastAccess != null)
                    {
                        //check if the last access is in or out
                        if (lastAccess.Direction == Core.Models.AccessDirections.In)
                            return Ok(plate); // if last access is "in" then ok, otherwise NotFound
                    }
                }
            }

            return NotFound();
        }

        // Recognize text from a remote image
        private async Task<TextOperationResult> ExtractRemoteTextAsync(ComputerVisionClient computerVision, string imageUrl)
        {
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                Debug.WriteLine("\nInvalid remoteImageUrl:\n{0} \n", imageUrl);
                return null;
            }

            // Start the async process to recognize the text
            RecognizeTextHeaders textHeaders = await computerVision.RecognizeTextAsync(imageUrl, textRecognitionMode);

            // Retrieve the URI where the recognized text will be
            // stored from the Operation-Location header
            string operationId = textHeaders.OperationLocation.Substring(textHeaders.OperationLocation.Length - numberOfCharsInOperationId);

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