using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Parkmeter.Core.Models;
using Parkmeter.Data;
using Parkmeter.Persistence;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cors;

//useful swagger tips 
//http://michaco.net/blog/TipsForUsingSwaggerAndAutorestInAspNetCoreMvcServices

namespace Parkmeter.Api.Controllers
{
   
    [Route("[controller]")]    
    public class ParkingController : Controller
    {
        private PersistenceManager _store;
        private IConfiguration _configuration;

        public ParkingController(PersistenceManager store, IConfiguration configuration)
        {
            _configuration = configuration;
            _store = store;
            _store.Initialize(
                new Uri(_configuration["DocumentDB:Endpoint"]),
                _configuration["DocumentDB:Key"],
                _configuration["ConnectionStrings:Default"]);          
        }

        #region Parking
        [HttpGet("{parkingId}")]
        [SwaggerOperation(operationId: "GetParking")] //for autorest
        [Produces("application/json", Type = typeof(Parking))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult Get(int parkingId)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();

            var parking = _store.GetParking(parkingId);
            if (parking == null)
                return NotFound();
            else
                return Json(parking);
        }

        [HttpDelete("{parkingId}")]
        [SwaggerOperation(operationId: "DeleteParking")] //for autorest
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult Delete(int parkingId)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();

            try
            {               
                Parking p = _store.GetParking(parkingId);
                if (p == null)
                    return NotFound();
                else
                    _store.DeleteParking(p);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return BadRequest();
            }
            return Ok();
        }

        [HttpPut]
        [SwaggerOperation(operationId: "CreateParking")] //for autorest
        [Produces("application/json", Type = typeof(PersistenceResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public IActionResult Put(string parkingName)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (String.IsNullOrEmpty(parkingName)) return BadRequest();

            var res = _store.AddNewParking(parkingName);
            var resJson = Json(res);

            return resJson;
        }


        [HttpGet("List")]
        [SwaggerOperation(operationId: "GetParkingsList")] //for autorest
        [Produces("application/json", Type = typeof(List<Parking>))]
        public IActionResult Get()
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            var list = _store.ParkingsStore.List();
            return Json(list);
        }
        #endregion

        #region Spaces

        [SwaggerOperation(operationId: "CreateSpace")] //for autorest
        [HttpPut("space/create")]
        public IActionResult Put(Space s)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");
            
            _store.SpacesStore.Add(s);
            return Ok();
        }

        [SwaggerOperation(operationId: "CreateSpaces")] //for autorest
        [HttpPut("space/create/{parkingId}/{number}")]
        public IActionResult Post(int parkingId, int number)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");
            try
            {
                while (number-- > 0)
                {
                    _store.SpacesStore.Add(new Space()
                    {
                        ParkingID = parkingId,
                        SpecialAttribute = SpecialAttributes.None,
                        VehicleType = VehicleTypes.Car
                    });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "spaces not persisted");                
            }
            return Ok();
        }
        #endregion

        #region In/Out/Status

        [HttpGet("Status/{parkingId}")]
        [SwaggerOperation(operationId: "GetParkingStatus")] //for autorest
        [Produces("application/json", Type = typeof(ParkingStatus))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetStatus(int parkingId)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();
       
            var parking = _store.GetParking(parkingId);
            if (parking == null)
                return NotFound();

            ParkingStatus status = await _store.GetParkingStatus(parkingId);
            return Json(status);
        }


        [HttpPut("{parkingId}/in/{vehicleId}")]
        [SwaggerOperation(operationId: "RegisterVehicleIn")] //for autorest        
        public async Task<IActionResult> VehicleIn(int parkingId, string vehicleId)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();

            try
            {
                var firstSpace = _store.GetParking(parkingId).Spaces.First();

                await _store.RegisterAccessAsync(new VehicleAccess()
                    { Direction = AccessDirections.In,
                    ParkingID = parkingId,
                    SpaceID = firstSpace.ID,
                    TimeStamp = DateTime.Now,
                    VehicleID = vehicleId,
                    VehicleType = VehicleTypes.Car
                    });
                
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        [HttpPut("{parkingId}/out/{vehicleId}")]
        [SwaggerOperation(operationId: "RegisterVehicleOut")] //for autorest      
        public async Task<IActionResult> VehicleOut(int parkingId, string vehicleId)
        {
            if (!_store.IsInitialized)
                return StatusCode(StatusCodes.Status500InternalServerError, "Not initialized");

            if (parkingId <= 0)
                return BadRequest();

            var firstSpace = _store.GetParking(parkingId).Spaces.First();

            await _store.RegisterAccessAsync(new VehicleAccess()
            {
                Direction = AccessDirections.Out,
                ParkingID = parkingId,
                SpaceID = firstSpace.ID,
                TimeStamp = DateTime.Now,
                VehicleID = vehicleId,
                VehicleType = VehicleTypes.Car
            });

            return Ok();
        }
        #endregion
    }
}
