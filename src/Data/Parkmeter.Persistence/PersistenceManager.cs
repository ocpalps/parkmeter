using Parkmeter.Core.Interfaces;
using Parkmeter.Core.Models;
using Parkmeter.Data.EF;
using Parkmeter.Data.NoSql;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Parkmeter.Persistence
{
    public class PersistenceManager
    {
        public bool IsInitialized { get; private set; }

        public IRepository<Parking> ParkingsStore { get; private set; }
        public IRepository<Space> SpacesStore { get; private set; }
        public ILedger AccessLedger { get; private set; }



        public void Initialize(Uri functionsEndpoint, string sqlConnectionString)
        {
            if (IsInitialized) return;

            if (functionsEndpoint == null)
                throw new InvalidDataException("DocumentDB settings are invalid");

            if (String.IsNullOrEmpty(sqlConnectionString))
                throw new InvalidDataException("Sql settings are invalid");

            try
            {
                //EF initialization
                EFRepositoryFactory factory = new EFRepositoryFactory();
                ParkingsStore = factory.CreateRepository<Parking>(sqlConnectionString);
                SpacesStore = factory.CreateRepository<Space>(sqlConnectionString);

                //NoSql initialization

                //this class expect a configuration

                AccessLedger = new LedgerServerLess(functionsEndpoint);

                AccessLedger.Initialize();

                IsInitialized = AccessLedger.IsInizialized;
            }
            catch (Exception)
            {
                IsInitialized = false;
            }
        }

      
        #region "Parking methods"
        public PersistenceResult DeleteParking(int parkingID)
        {
            var p = GetParking(parkingID);
            if (p == null)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = $"Parking with ID {parkingID} not found!" };
            }


            return DeleteParking(p);
        }

        public PersistenceResult DeleteParking(Parking parking)
        {
            try
            {
                var p = GetParking(parking.ID);
                ParkingsStore.Delete(p);
            }
            catch (Exception ex)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }

            return new PersistenceResult() { State = ResultStates.Completed };
        }

        public Parking GetParking(int parkingID)
        {
            return ParkingsStore.GetById(parkingID, new string[] { "Spaces" });
        }
        public PersistenceResult AddNewParking(string parkingName)
        {
            if (String.IsNullOrEmpty(parkingName)) return new PersistenceResult() {State = ResultStates.Error, Message = "Parking name null or empty" };

            int newID = -1;

            try
            {
                ParkingsStore.Add(new Parking() { Name = parkingName });
                newID = ParkingsStore.List().LastOrDefault().ID;
            }
            catch (Exception ex)
            {
                return new PersistenceResult() {State = ResultStates.Error, Message = ex.Message };
            }

            return new PersistenceResult() { ID = newID, State = ResultStates.Completed };
        }

        public PersistenceResult AddSpaceToParking(Space space, int parkingID)
        {
            var p = GetParking(parkingID);
            if (p == null) return new PersistenceResult() {State = ResultStates.Error, Message = $"Parking with ID {parkingID} not found!" };

            return AddSpaceToParking(space, p);
        }
        public PersistenceResult AddSpaceToParking(Space space, Parking parking)
        {
            int newID = -1;

            if (space == null || parking == null)
                return new PersistenceResult() { ID = newID, State = ResultStates.Error, Message = "Space or Parking null reference" };

            try
            {
                parking.AddSpace(space);
                ParkingsStore.Update(parking);
                return new PersistenceResult() {ID =parking.Spaces.Last().ID, State= ResultStates.Completed };
            }
            catch (Exception ex)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }
#pragma warning disable CS0162 // Unreachable code detected
            return new PersistenceResult() { ID=newID , State = ResultStates.Completed };
#pragma warning restore CS0162 // Unreachable code detected
        }
        #endregion

        #region "Space methods"
        public Space GetSpace(int spaceID)
        {
            return SpacesStore.GetById(spaceID, new string[] { "Parking" });
        }
        public PersistenceResult DeleteSpace(int spaceID)
        {
            var s = SpacesStore.GetById(spaceID, new string[] { "Parking" });
            if (s == null)
                return new PersistenceResult() { State = ResultStates.Error, Message = $"Space with ID {spaceID} not found!" };

            return DeleteSpace(s);
        }
        public PersistenceResult DeleteSpace(Space space)
        {
            try
            {
                var p = GetParking(space.ParkingID);
                p.DeleteSpace(space.ID);
                ParkingsStore.Update(p);
            }
            catch (Exception ex)
            {
                return new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }

            return new PersistenceResult() { State = ResultStates.Completed };
        }
        #endregion

        #region "Ledger methods"

        public async Task<ParkingStatus> GetParkingStatus(int parkingID)
        {
            ParkingStatus status = null;
            try
            {
                //get current status from the ledger
                status = await AccessLedger.GetParkingStatusAsync(parkingID);

                //if there isn't a status in the ledger, return a new blank status
                if (status == null)
                {
                    status = new ParkingStatus()
                    {
                        ParkingId = parkingID,
                        BusySpaces = 0
                    };
                }

                //get total spaces from db
                status.TotalAvailableSpaces = GetParking(parkingID).Spaces.Count;
            }
            catch (Exception ex)
            {
                
            }

            return status;
        }

        public async Task<VehicleAccess> GetLastVehicleAccess(int parkingID, string vehicleId)
        {
            VehicleAccess access = null;
            try
            {
                //get last access of a specific vehicle from the ledger
                access = await AccessLedger.GetLastVehicleAccessAsync(parkingID, vehicleId);

                //if there isn't an access in the ledger, return null
                return access;
            }
            catch (Exception ex)
            {

            }

            return access;
        }

        public async Task<PersistenceResult> RegisterAccessAsync(VehicleAccess access)
        {
            PersistenceResult result;

            try
            {
                result = await AccessLedger.RegisterAccessAsync(access);
            }
            catch (Exception ex)
            {
                result = new PersistenceResult() { State = ResultStates.Error, Message = ex.Message };
            }

            return result;
        }
        #endregion

    }
}
