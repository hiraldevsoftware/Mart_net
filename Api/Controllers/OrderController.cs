using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Enums;
using Mart.Domain.Interface;
using Mart.Domain.ValueObjects;
using Mart.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }


        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {

                var order = new Order(
                    request.UserId,
                    request.StoreId,
                    new Money(request.TotalAmount, "INR")
                );

                foreach (var item in request.Items)
                {
                    order.AddItem(item.ProductId, item.Quantity, new Money(item.UnitPrice, "INR"));
                }

                int orderId = await _orderRepository.PlaceOrderAsync(order);

                return Ok(new
                {
                    success = true,
                    orderId = orderId,
                    message = "Order placed! Delivery in 10 mins."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetHistory(int userId)
        {
            var history = await _orderRepository.GetOrderHistoryAsync(userId);
            return Ok(history);
        }



        [HttpGet("track/{orderId}")]
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            var tracking = await _orderRepository.GetOrderTrackingAsync(orderId);
            if (tracking == null) return NotFound(new { message = "Order not found" });
            return Ok(tracking);
        }

        [HttpPost("one-tap")]
        public async Task<IActionResult> OneTapCheckout([FromBody] OneTapRequest request)
        {
            try
            {
           
                var orderId = await _orderRepository.PlaceOneTapOrderAsync(request.UserId, request.StoreId, request.ProductId);

                return Ok(new
                {
                    success = true,
                    orderId = orderId,
                    message = "One-Tap Order Placed! No address selection needed."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        [HttpPost("scheduled-checkout")]
        public async Task<IActionResult> ScheduledCheckout([FromBody] ScheduledCheckoutRequest request)
        {
  
            if (string.IsNullOrEmpty(request.RazorpayPaymentId))
            {
                return BadRequest(new { message = "Razorpay Payment ID Is compulsory" });
            }

            if (request.ScheduledDate.Date < DateTime.Now.Date)
            {
                return BadRequest(new { message = "Delievery Is Not Possible For Past Date" });
            }

            try
            {
                int orderId = await _orderRepository.PlaceScheduledOrderAsync(request);

                return Ok(new
                {
                    success = true,
                    orderId = orderId,
                    message = $"Order Scheduled for {request.ScheduledDate:dd-MM-yyyy} successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("available-slots")]
        public async Task<IActionResult> GetSlots([FromQuery] DateTime date)
        {
            try
            {

                var selectedDate = date == default ? DateTime.Now : date;

                var slots = await _orderRepository.GetAvailableSlotsAsync(selectedDate);

                if (!slots.Any())
                {
                    return Ok(new { message = "Bhai, aa divas mate koi slot khali nathi!", data = new List<object>() });
                }

                return Ok(slots);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
