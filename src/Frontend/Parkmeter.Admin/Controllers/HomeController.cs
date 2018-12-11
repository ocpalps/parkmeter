using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using Parkmeter.Admin.Models;
using Parkmeter.Admin.ViewModels;
using Parkmeter.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Parkmeter.SDK.Models;
//#### STEP 05
using Microsoft.AspNetCore.Authorization;

namespace Parkmeter.Admin.Controllers
{
    //#### STEP 05
    [Authorize]
    public class HomeController : Controller
    {
        private async Task<IParkmeterApi> InitializeClient()
        {
            IParkmeterApi _apiClient = new ParkmeterApi();
            _apiClient.BaseUri = new Uri(Configuration["Parkmeter:ApiUrl"]);

            return _apiClient;
        }

        IConfiguration Configuration { get; set; }

        public HomeController(IConfiguration config)
        {
            Configuration = config;
        }

        public async Task<IActionResult> Index()
        {
            var _apiClient = await InitializeClient();
            if (_apiClient == null) return Error();

            var parkings = _apiClient.GetParkingsList();
            var parkingViewModels = new List<ParkingViewModel>();
            foreach (var p in parkings)
            {
                parkingViewModels.Add(new ParkingViewModel() { Parking = p, Status = _apiClient.GetParkingStatus(p.Id.Value) });
            }

            ViewBag.Parkings = parkingViewModels;
         
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult CreateNewParking()
        {
            return View();
        }

        
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewParking([Bind("Name,TotalAvailableSpaces")] NewParkingViewModel parking)
        {
            if (ModelState.IsValid)
            {            
                var _apiClient = await InitializeClient();
                if (_apiClient == null) return Error();

                var newParking = _apiClient.CreateParking(parking.Name);
                _apiClient.CreateSpaces(newParking.Id.Value, parking.TotalAvailableSpaces);
                return RedirectToAction(nameof(Index));
            }
            return View(parking);
        }

        public async Task<IActionResult> RemoveParking(int parkingId)
        {
            if (ModelState.IsValid)
            {                
                var _apiClient = await InitializeClient();
                if (_apiClient == null) return Error();

                _apiClient.DeleteParking(parkingId);
              
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> RegisterIn(int parkingId)
        {
            if (ModelState.IsValid)
            {
                var _apiClient = await InitializeClient();
                if (_apiClient == null) return Error();

                _apiClient.RegisterVehicleIn(parkingId,"AA12345");

            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> RegisterOut(int parkingId)
        {
            if (ModelState.IsValid)
            {
                var _apiClient = await InitializeClient();
                if (_apiClient == null) return Error();

                _apiClient.RegisterVehicleOut(parkingId, "AA12345");

            }
            return RedirectToAction("Index");
        }



    }
}
