using Mart.Domain.Enums;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IAdminOrderRepository _orderRepo;

        public AdminOrderController(IAdminOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }


        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var orders = await _orderRepo.GetPendingOrdersAsync();
            return Ok(orders);
        }


        [HttpPatch("{id}/accept")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var result = await _orderRepo.UpdateOrderStatusAsync(id, OrderStatus.Accepted);
            return result ? Ok("Order Accepted. Status moved to Processing.") : BadRequest("Failed to accept order.");
        }

        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectOrder(int id)
        {
            var result = await _orderRepo.UpdateOrderStatusAsync(id, OrderStatus.Rejected);
            return result ? Ok("Order Rejected.") : BadRequest("Failed to reject order.");
        }


        [HttpGet("available-riders")]
        public async Task<IActionResult> GetRiders()
        {
            var riders = await _orderRepo.GetAvailableRidersAsync();
            return Ok(riders);
        }



        [HttpPost("{orderId}/assign-rider")]
        public async Task<IActionResult> AssignRider(int orderId, [FromBody] int riderId)
        {
            if (riderId <= 0) return BadRequest("Invalid Rider ID");

            var result = await _orderRepo.AssignRiderAsync(orderId, riderId);

            if (result)
                return Ok(new { Message = $"Rider assigned successfully and Order is now Out for Delivery!" });

            return BadRequest("Failed to assign rider.");
        }


        [HttpGet("{orderId}/details")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {

            var result = await _orderRepo.GetOrderDetailsByIdAsync(orderId);

            if (result == null)
            {
                return NotFound(new { Message = $"Order with ID {orderId} not found." });
            }

            return Ok(result);
        }


        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _orderRepo.GetDashboardStatsAsync();
            return Ok(stats);
        }
    }
}                                                                            
                                                                    



                                                                     