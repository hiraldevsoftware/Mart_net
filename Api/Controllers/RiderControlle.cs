namespace Mart.Api.Controllers
{
    using global::Mart.Api.Models;
    using global::Mart.Domain.Interface;

    using Microsoft.AspNetCore.Mvc;

    namespace Mart.Api.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class RiderController : ControllerBase
        {
            private readonly IRiderRepository _riderRepo;

            public RiderController(IRiderRepository riderRepo)
            {
                _riderRepo = riderRepo;
            }


            [HttpPut("{riderId}/status")]
            public async Task<IActionResult> UpdateStatus(int riderId, [FromBody] bool isAvailable)
            {
                var result = await _riderRepo.UpdateRiderAvailabilityAsync(riderId, isAvailable);

                if (result)
                    return Ok(new { Message = isAvailable ? "Rider is now Online" : "Rider is now Offline" });

                return BadRequest("Failed to update status.");
            }


            [HttpPost("respond-to-order")]
            public async Task<IActionResult> RespondToOrder(int orderId, int riderId, string response)
            {

                var result = await _riderRepo.RespondToAssignmentAsync(orderId, riderId, response);

                if (result)
                {
                    string msg = response.Equals("Accepted", StringComparison.OrdinalIgnoreCase)
                                 ? "Order successfully assigned to you!"
                                 : "Order rejected.";
                    return Ok(new { Message = msg });
                }

                return BadRequest("Assignment expired, already responded, or invalid request.");
            }


            [HttpPost("{riderId}/location")]
            public async Task<IActionResult> UpdateLocation(int riderId, [FromBody] LocationDto location)
            {
                await _riderRepo.UpdateLocationAsync(riderId, location.Latitude, location.Longitude);
                return Ok(new { Message = "Location updated" });
            }


            [HttpPost("start-waiting")]
            public async Task<IActionResult> StartWaiting(int orderId, [FromBody] LocationDto location)
            {
                var result = await _riderRepo.StartWaitingTimerAsync(orderId, location.Latitude, location.Longitude);

                if (result)
                    return Ok(new { Message = "Waiting timer started successfully." });

                return BadRequest("You are too far from the customer location to start the timer.");
            }


            [HttpPost("verify-delivery")]
            public async Task<IActionResult> VerifyDelivery(int orderId, string otp)
            {
                var result = await _riderRepo.VerifyDeliveryOTPAsync(orderId, otp);

                if (result)
                    return Ok(new { Message = "Order delivered successfully! OTP Verified." });

                return BadRequest("Invalid OTP or Order already processed.");
            }



            [HttpPost("adjust-inventory")]
            public async Task<IActionResult> AdjustInventory([FromBody] InventoryAdjustmentDto dto)
            {
                var result = await _riderRepo.AdjustInventoryAsync(
                    dto.ProductId,
                    dto.StoreId,
                    dto.QuantityChange,
                    dto.Reason,
                    dto.StaffId
                );

                if (result)
                    return Ok(new { Message = "Inventory adjusted and logged successfully." });

                return BadRequest("Failed to adjust inventory.");
            }

            [HttpGet("{riderId}/earnings")]
            public async Task<IActionResult> GetEarnings(int riderId)
            {
                var total = await _riderRepo.GetTotalEarningsAsync(riderId);
                return Ok(new { RiderId = riderId, TotalEarnings = total });
            }



            [HttpPost("update-profile")]
            public async Task<IActionResult> UpdateProfile(int riderId, string licenseNo, string rcBookUrl)
            {
                var result = await _riderRepo.UpdateRiderProfileAsync(riderId, licenseNo, rcBookUrl);
                if (result) return Ok(new { message = "Profile updated for verification." });
                return BadRequest(new { message = "Failed to update profile." });
            }



            [HttpPost("pickup-order/{orderId}")]
            public async Task<IActionResult> PickUpOrder(int orderId)
            {
                try
                {

                    var result = await _riderRepo.MarkOrderAsPickedUpAsync(orderId);

                    if (result)
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Order Pick up from Store. You going to deliver",
                            orderStatus = 3 
                        });
                    }
                    else
                    {

                        return BadRequest(new
                        {
                            success = false,


                           message= "Something is wrong in order pick up. OrderId is wrong or Order is nort ready"
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Server Error: " + ex.Message });
                }
            }





            [HttpGet("customer-phone/{orderId}")]
            public async Task<IActionResult> GetCustomerPhone(int orderId)
            {
                try
                {
                    var phoneNumber = await _riderRepo.GetCustomerPhoneForOrderAsync(orderId);

                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        return Ok(new
                        {
                            success = true,
                            phoneNumber = phoneNumber
                        });
                    }
                    else
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "For This Order Not getting Customer Number "
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = "Error: " + ex.Message });
                }
            }




            [HttpPost("broadcast-order/{orderId}")]
            public async Task<IActionResult> BroadcastOrder(int orderId, [FromQuery] int storeId, [FromQuery] double radius = 5000)
            {
                try
                {

                    var nearbyRiders = await _riderRepo.GetAvailableRidersInRangeAsync(storeId, radius);

                    if (nearbyRiders == null || !nearbyRiders.Any())
                    {
                        return NotFound(new { Message = "Currently no riders available in this area." });
                    }


                    foreach (var rider in nearbyRiders)
                    {
                        await _riderRepo.CreateAssignmentAsync(orderId, rider.RiderId);

                    }

                    return Ok(new
                    {
                        Message = $"Order broadcasted to {nearbyRiders.Count()} riders.",
                        RiderCount = nearbyRiders.Count()
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }




            [HttpPost("accept-order")]
            public async Task<IActionResult> AcceptOrder([FromBody] AcceptOrderDto dto)
            {
                try
                {
                    // 1. Validation: Pehla check karo ke Rider pase cash limit baki che?
                    bool isUnderLimit = await _riderRepo.IsRiderUnderLimitAsync(dto.RiderId);
                    if (!isUnderLimit)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "Tame cash limit cross kari didhi che. Pehla dukan par cash jama karo!"
                        });
                    }

                    // 2. Action: Order assign karvani koshish karo
                    // RespondToAssignmentAsync ma apde check karishu ke order 'Pending' che ke nahi
                    var result = await _riderRepo.RespondToAssignmentAsync(dto.OrderId, dto.RiderId, "Accepted");

                    if (result)
                    {
                        return Ok(new
                        {
                            Success = true,
                            Message = "Order successfully assigned to you. Prepare for pickup!"
                        });
                    }
                    else
                    {
                        // Jo result false aave, eno matlab ke order koi bija rider e pehla upadi lidho che
                        return Conflict(new
                        {
                            Success = false,
                            Message = "Oops! Aa order bija koi rider e pehla accept kari lidho che."
                        });
                    }
                }
                catch (Exception ex)
                {
                    // System error handle karva mate
                    return StatusCode(500, new { Success = false, Message = "Server error: " + ex.Message });
                }
            }




            [HttpPost("complete-delivery")]
            public async Task<IActionResult> CompleteDelivery([FromBody] DeliveryCompletionDto dto)
            {

                bool isOtpValid = await _riderRepo.VerifyDeliveryOTPAsync(dto.OrderId, dto.Otp);

                if (!isOtpValid) return BadRequest("Khoto OTP che!");

     
                if (dto.PaymentMode == "COD")
                {
                    bool cashCollected = await _riderRepo.CollectCashAsync(dto.RiderId, dto.OrderId, dto.Amount);

                    if (!cashCollected) return StatusCode(500, "Cash record update ma bhul aavi.");
                }
                else
                {
   
                }

                return Ok("Order Delivered Successfully!");
            }



            [HttpPost("update-order-stage")]
            public async Task<IActionResult> UpdateOrderStage([FromBody] UpdateStageDto dto)
            {
                try
                {

                    bool isUpdated = await _riderRepo.UpdateOrderStageAsync(dto.OrderId, dto.Stage, dto.Lat, dto.Lng);

                    if (isUpdated)
                    {
                        return Ok(new
                        {
                            Success = true,
                            Message = $"Order stage updated to {dto.Stage} successfully."
                        });
                    }

                    return BadRequest(new { Success = false, Message = "Failed to update order stage." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Success = false, Message = "Server error: " + ex.Message });
                }
            }




            [HttpPost("reject-order")]
            public async Task<IActionResult> RejectOrder(int riderId, int orderId)
            {
                try
                {
                    var message = await _riderRepo.RejectOrderAsync(riderId, orderId);

     

                    return Ok(new { Success = true, Message = message });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Success = false, Message = ex.Message });
                }
            }

        }






        public class LocationDto
        {
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
        }


        public class InventoryAdjustmentDto
        {
            public int ProductId { get; set; }
            public int StoreId { get; set; }
            public int QuantityChange { get; set; }
            public string Reason { get; set; } 
            public int StaffId { get; set; }
        }
    }
}
