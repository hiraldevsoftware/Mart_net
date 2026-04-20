using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderAssignmentController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;

        public RiderAssignmentController(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToAssignment(int orderId, int riderId, string response)
        {
            // 1. Check karo ke 60 seconds thai gaya che ke nahi
            // 2. Jo 'Accepted' hoy to Orders table ma RiderId set karo ane status badlo
            // 3. Jo 'Rejected' hoy to bijo rider shodho

            bool success = await _orderRepo.UpdateRiderAssignmentAsync(orderId, riderId, response);

            if (success) return Ok(new { message = "Status updated successfully!" });
            return BadRequest("Assignment expired or invalid.");
        }
    }
}
