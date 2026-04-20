using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IStoreRepository _storeRepository;

        public LocationController(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        [HttpGet("check-service")]
        public async Task<IActionResult> CheckService([FromQuery] decimal lat, [FromQuery] decimal lon)
        {
            var nearestStore = await _storeRepository.GetNearestStoreAsync(lat, lon);

            if (nearestStore == null || nearestStore.DistanceInKm > nearestStore.ServiceRadiusInKm)
            {
                return Ok(new
                {
                    isServiceable = false,
                    message = "Sorry! We are not delivering at your location yet.",
                    distance = nearestStore != null ? Math.Round(nearestStore.DistanceInKm, 2) : 0,
                    area = "Unknown"
                });
            }

            return Ok(new
            {
                isServiceable = true,
                storeId = nearestStore.Id,
                storeName = nearestStore.StoreName,
                distance = Math.Round(nearestStore.DistanceInKm, 2),
                estimatedDeliveryTime = "10-15 Mins",
                message = "Yay! We are delivering in your area."
            });
        }
    }
}
