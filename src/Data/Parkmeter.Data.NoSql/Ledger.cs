using Parkmeter.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Parkmeter.Core.Models;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Parkmeter.Data.NoSql
{
    public class Ledger : ILedger
    {
        private const string UPDATE_PARKING_STATUS_PATH = "Parkmeter.Data.NoSql.Triggers.UpdateParkingStatus.js";

        public bool IsInizialized { get; private set; }
        public Uri Endpoint { get; set; }
        public String Key { get; set; }

        public Ledger(Uri endpoint, string key)
        {
            if (endpoint == null || String.IsNullOrEmpty(key))
                throw new InvalidDataException("DocumentDB settings are invalid");

            Endpoint = endpoint;
            Key = key;
        }

        public async void Initialize()
        {
            IsInizialized = DocumentDBRepository<ParkingStatus>.Initialize(Endpoint,Key);

            IsInizialized &= DocumentDBRepository<VehicleAccess>.Initialize(Endpoint, Key);

            try
            {
                await DocumentDBRepository<VehicleAccess>.CreateTriggerAsync(UPDATE_PARKING_STATUS_PATH, "UpdateParkingStatus");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ledger not initialized",ex.Message);
                IsInizialized = false;
            }
             
        }

        public async Task<PersistenceResult> RegisterAccessAsync(VehicleAccess access)
        {
            if (!IsInizialized)
                return new PersistenceResult() { State = ResultStates.Error, Message="Not initialized!" };

            try
            {
                await DocumentDBRepository<VehicleAccess>.CreateItemAsync(access);
            }
            catch (Exception ex)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }

            return new PersistenceResult() { State = ResultStates.Completed };
        }

        public async Task<ParkingStatus> GetParkingStatusAsync(int parkingId)
        {
            if (!IsInizialized)
                return null;

            ParkingStatus status =  await DocumentDBRepository<ParkingStatus>.GetItemAsync("_status_" + parkingId);

            return status;
        }
    }
}
