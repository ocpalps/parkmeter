using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Parkmeter.Functions
{
    public class EventGridHelper
    {

        public enum EventType
        {
            Succeeded,
            Error
        }
        public class ParkmeterEvent
        {
            public String Message { get; set; }
            public EventType Type { get; set; }
            public String Data { get; set; }
        }
        private static HttpClient _eventGridClient = null;

        public static async Task SendEvent(ParkmeterEvent @event)
        {
            string eventGridUri = System.Environment.GetEnvironmentVariable("EventGridUri", EnvironmentVariableTarget.Process);
            string eventGridKey = System.Environment.GetEnvironmentVariable("EventGridKey", EnvironmentVariableTarget.Process);

            if (_eventGridClient == null)
            {
                
                if (String.IsNullOrWhiteSpace(eventGridKey) || String.IsNullOrWhiteSpace(eventGridKey))
                {
                    Trace.TraceError("Event grid tracing is not configured");
                    return;
                }
                _eventGridClient = new HttpClient();
                _eventGridClient.BaseAddress = new Uri(eventGridUri);
                _eventGridClient.DefaultRequestHeaders.Add("aeg-sas-key", eventGridKey);
            }

            try
            {
                var egevent = new
                {
                    Id = Guid.NewGuid(),
                    Subject = @event.Message,
                    EventType = @event.Type == EventType.Error ? "parkmeter.event.error" : "parkmeter.event.succeeded",
                    EventTime = DateTime.UtcNow,
                    Data = @event.Data
                };
                var x = await _eventGridClient.PostAsJsonAsync("", new[] { egevent });
                x.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Event grid error: ", ex.Message);
            }
        }
    }
}
