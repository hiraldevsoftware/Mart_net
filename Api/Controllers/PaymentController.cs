using Mart.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController: ControllerBase
    {
        private readonly string _keyId = "YOUR_RAZORPAY_KEY";
        private readonly string _keySecret = "YOUR_RAZORPAY_SECRET";

        [HttpPost("create-razorpay-order")]
        public IActionResult CreateOrder([FromBody] decimal amount)
        {
            try
            {
                RazorpayClient client = new RazorpayClient(_keyId, _keySecret);

                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", amount * 100); 
                options.Add("currency", "INR");
                options.Add("receipt", Guid.NewGuid().ToString());

               Razorpay.Api.Order order = client.Order.Create(options);

                return Ok(new
                {
                    orderId = order["id"].ToString(),
                    amount = amount,
                    currency = "INR"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
