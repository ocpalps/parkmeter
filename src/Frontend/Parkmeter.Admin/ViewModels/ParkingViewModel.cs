using Parkmeter.SDK.Models;

namespace Parkmeter.Admin.ViewModels
{
    public class ParkingViewModel
    {
        public ParkingViewModel()
        {

        }
        public Parking Parking { get; set; }
        public ParkingStatus Status { get; set; }

    }
}
