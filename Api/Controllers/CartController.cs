using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly IProductRepository _productRepo;

        public CartController(IDistributedCache cache, IProductRepository productRepo)
        {
            _cache = cache;
            _productRepo = productRepo;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            try
            {
        
                var cartItems = await _productRepo.GetUserCartAsync(userId);

                if (cartItems == null || !cartItems.Any())
                {
                    return Ok(new { items = new List<object>() });
                }

                return Ok(new { items = cartItems });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error fetching cart", details = ex.Message });
            }
        }

        [HttpPost("sync/{userId}")]
        public async Task<IActionResult> SyncCart(int userId, [FromBody] object cartItems)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) // 30 divas sudhi cart rehse
            };

            await _cache.SetStringAsync($"cart:{userId}", JsonSerializer.Serialize(cartItems), options);
            return Ok(new { message = "Cart synced with Redis!" });
        }
    }
}
