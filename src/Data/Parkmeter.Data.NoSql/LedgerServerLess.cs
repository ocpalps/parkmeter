using Parkmeter.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Parkmeter.Core.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace Parkmeter.Data.NoSql
{
    public class LedgerServerLess : ILedger
    {
        
        public bool IsInizialized { get; private set; }
        public Uri Endpoint { get; set; }

        public LedgerServerLess(Uri endpoint)
        {
            if (endpoint == null)
                throw new InvalidDataException("DocumentDB settings are invalid");

            Endpoint = endpoint;
        }

        public async void Initialize()
        {
            IsInizialized = true;

        }

        public async Task<PersistenceResult> RegisterAccessAsync(VehicleAccess access)
        {
            if (!IsInizialized)
                return new PersistenceResult() { State = ResultStates.Error, Message="Not initialized!" };


            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = Endpoint;
                    var jsonObject = JsonConvert.SerializeObject(access);
                    var content = new StringContent(jsonObject, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync("registeraccess", content);
                    result.EnsureSuccessStatusCode();
                    return new PersistenceResult() { State = ResultStates.Completed };
                }
                catch (Exception ex)
                {
                    return new PersistenceResult() { State = ResultStates.Error };
                }
            }
        }

        public async Task<ParkingStatus> GetParkingStatusAsync(int parkingId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = Endpoint;
                    var result = await client.GetAsync("getparkingstatus/" + parkingId);
                    result.EnsureSuccessStatusCode();
                    ParkingStatus status = JsonConvert.DeserializeObject<ParkingStatus>(await result.Content.ReadAsStringAsync());
                    return status;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        public async Task<VehicleAccess> GetLastVehicleAccessAsync(int parkingId, string vehicleId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = Endpoint;
                    var result = await client.GetAsync($"getlastvehicleaccess/{parkingId}/{vehicleId}");
                    result.EnsureSuccessStatusCode();
                    VehicleAccess access = JsonConvert.DeserializeObject<VehicleAccess>(await result.Content.ReadAsStringAsync());
                    return access;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
    }
}
