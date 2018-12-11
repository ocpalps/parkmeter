using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Parkmeter.Admin.ViewModels
{
    public class NewParkingViewModel
    {
        public string Name { get; set; }
        [Display(Name = "Total Available Spaces")]
        public int TotalAvailableSpaces { get; set; }
    }
}
