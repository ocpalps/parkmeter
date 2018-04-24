using System;
namespace Parkmeter.Core.Models
{
    public enum AccessDirections
    {
        Out = -1,
        In = 1
    }

    public class VehicleAccess
    {
        public DateTime TimeStamp { get; set; }
        public int ParkingID { get; set; }
        public int SpaceID { get; set; }
        public AccessDirections Direction { get; set; }
        public string VehicleID { get; set; }
        public VehicleTypes VehicleType { get; set; }
    }
}
