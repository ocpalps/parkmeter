using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Core.Models
{
    public class ParkingStatus
    {
        public int ParkingId { get; set; }
        public int TotalAvailableSpaces { get; set; }

        public int BusySpaces { get; set; }

        public int FreeSpaces { get { return TotalAvailableSpaces - BusySpaces; } }

        public decimal FreePercentage
        {
            get
            {
                if (TotalAvailableSpaces == 0) return 0;

                return (decimal)FreeSpaces / (decimal)TotalAvailableSpaces * 100.0m;
            }
        }
        public decimal BusyPercentage
        {
            get
            {
                if (TotalAvailableSpaces == 0) return 0;
                return (decimal)BusySpaces / (decimal)TotalAvailableSpaces * 100.0m;
            }
        }

    }
}
