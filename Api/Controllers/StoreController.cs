using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreRepository _storeRepo;

        public StoreController(IStoreRepository storeRepo)
        {
            _storeRepo = storeRepo;
        }

        [HttpGet("pending-orders/{storeId}")]
        public async Task<IActionResult> GetPendingOrders(int storeId)
        {
            try
            {
                var orders = await _storeRepo.GetStorePendingOrdersAsync(storeId);

                if (orders == null || !orders.Any())
                {
                    return Ok(new { Message = "No pending orders found for this store.", Data = new List<dynamic>() });
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        //[HttpGet("inventory/{storeId}")]
        //public async Task<IActionResult> GetInventory(int storeId)
        //{
        //    var inventory = await _storeRepo.GetStoreInventoryAsync(storeId);
        //    return Ok(inventory);
        //}

        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory(int storeId, int staffId) 
        {
            try
            {
    
                bool isValid = await _storeRepo.IsStoreManagerValidAsync(storeId, staffId);

                if (!isValid)
                {
                    return Unauthorized(new { Message = "Unauthorized: User not assigned to this store." });
                }

                var inventory = await _storeRepo.GetStoreInventoryAsync(storeId);
                return Ok(inventory);
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        //[HttpPut("update-stock")]
        //public async Task<IActionResult> UpdateStock([FromBody] StockUpdateDto dto)
        //{
        //    var result = await _storeRepo.UpdateStoreStockAsync(dto.StoreId, dto.ProductId, dto.NewQuantity);
        //    if (result) return Ok(new { Message = "Stock updated successfully" });
        //    return BadRequest("Failed to update stock");
        //}


        [HttpPut("update-stock")]
        public async Task<IActionResult> UpdateStock([FromBody] StockUpdateDto dto)
        {


            var result = await _storeRepo.UpdateStoreStockAsync(
                dto.StoreId,
                dto.ProductId,
                dto.NewQuantity,
                dto.StaffId,
                dto.Reason
            );

            if (result) return Ok(new { Message = "Stock updated with reason." });
            return BadRequest("Failed to update stock");
        }


        [HttpPost("accept/{orderId}")]
        public async Task<IActionResult> AcceptOrder(int orderId, [FromQuery] int storeId)
        {

            var result = await _storeRepo.UpdateOrderStatusAsync(orderId, storeId, 2);
            return result ? Ok(new { msg = "Order Accepted. Stock Deducted." }) : BadRequest();
        }


        [HttpPost("ready-for-pickup/{orderId}")]
        public async Task<IActionResult> ReadyForPickup(int orderId, [FromQuery] int storeId)
        {

            var result = await _storeRepo.UpdateOrderStatusAsync(orderId, storeId, 5);
            return result ? Ok(new { msg = "Order is Ready. Notification sent to Rider." }) : BadRequest();
        }


        [HttpPost("reject/{orderId}")]
        public async Task<IActionResult> RejectOrder(int orderId, [FromQuery] int storeId)
        {

            var result = await _storeRepo.UpdateOrderStatusAsync(orderId, storeId, 3);
            return result ? Ok(new { msg = "Order Rejected." }) : BadRequest();
        }


        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var details = await _storeRepo.GetOrderItemsAsync(orderId);
            return Ok(details);
        }



        [HttpPut("update-location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
        {
            try
            {
                var result = await _storeRepo.UpdateProductLocationAsync(
                    dto.ProductId,
                    dto.StoreId,
                    dto.RackNumber,
                    dto.ShelfLevel
                );

                if (result)
                    return Ok(new { Message = "Product location updated successfully." });

                return BadRequest("Could not update location. Verify ProductId and StoreId.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet("verify-item")]
        public async Task<IActionResult> VerifyItem(int orderId, string barcode)
        {
            try
            {
                bool isCorrectItem = await _storeRepo.VerifyProductInOrderAsync(orderId, barcode);

                if (isCorrectItem)
                {
                    return Ok(new { Success = true, Message = "Sachi item che. Packing chalu rakho." });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Bhul che! Aa item aa order ma nathi." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost("request-stock")]
        public async Task<IActionResult> RequestStock([FromBody] StockRequestDto dto)
        {
            var result = await _storeRepo.CreateStockRequestAsync(dto.StoreId, dto.ProductId, dto.Qty, dto.StaffId);
            if (result) return Ok(new { Message = "Stock request sent to Admin." });
            return BadRequest("Failed to send request.");
        }

        [HttpGet("my-stock-requests/{storeId}")]
        public async Task<IActionResult> GetMyRequests(int storeId)
        {
            var requests = await _storeRepo.GetStoreStockRequestsAsync(storeId);
            return Ok(requests);
        }


        [HttpPost("perform-audit")]
        public async Task<IActionResult> PerformAudit([FromBody] AuditDto dto)
        {
            var result = await _storeRepo.PerformStockAuditAsync(dto.StoreId, dto.ProductId, dto.PhysicalQty, dto.StaffId, dto.Remarks);

            if (result)
                return Ok(new { Message = "Audit completed. Inventory updated." });

            return BadRequest("Audit failed.");
        }


    }
}
