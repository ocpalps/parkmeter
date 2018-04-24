using System;
using Parkmeter.Core.Models;
using System.Threading.Tasks;

namespace Parkmeter.Core.Interfaces
{
    public interface ILedger
    {
        bool IsInizialized { get; }
        void Initialize();
        Task<PersistenceResult> RegisterAccessAsync(VehicleAccess access);
        Task<ParkingStatus> GetParkingStatusAsync(int parkingId);
    }
}
