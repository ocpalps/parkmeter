using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.Functions
{
    public class ParkingStatusDocument
    {
        //"_status_" + doc.ParkingID,
        public string id { get; set; }
        public int ParkingID { get; set; }
        public bool isStatus { get; set; }
        public int busySpaces { get; set; }
    }
}
