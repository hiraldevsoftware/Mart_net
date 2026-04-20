using Mart.Api.Models;
using Mart.Domain.Interface;
using Mart.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController: ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IProductRepository _productRepo;

        public NotificationController(IUserRepository userRepo, IProductRepository productRepo)
        {
            _userRepo = userRepo;
            _productRepo = productRepo;
        }

        [HttpPost("update-fcm-token")]
        public async Task<IActionResult> UpdateToken([FromBody] FcmTokenRequest request)
        {
            await _userRepo.UpdateFcmTokenAsync(request.UserId, request.Token);
            return Ok(new { message = "Token updated successfully!" });
        }

        [HttpPost("notify-me")]
        public async Task<IActionResult> NotifyMe([FromBody] NotifyRequest request)
        {
            await _productRepo.RegisterForNotificationAsync(request.UserId, request.ProductId);
            return Ok(new { message = "Items Add In List Successfully!" });
        }
    }
}
