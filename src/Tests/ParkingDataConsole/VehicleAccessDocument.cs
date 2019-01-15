using Parkmeter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parkmeter.ParkingDataConsole
{
    public class VehicleAccessDocument
    {
        public VehicleAccess Access { get; set; }
        public VehicleAccessDocument() { }

        public string id { get; set; }
    }
}
