using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly IVendorRepository _vendorRepo;
        public VendorController(IVendorRepository vendorRepo) => _vendorRepo = vendorRepo;


        [HttpPost("create-po")]
        public async Task<IActionResult> CreatePO(int productId, int vendorId, decimal quantity, DateTime deliveryDate)
        {
            try
            {
                var id = await _vendorRepo.CreatePurchaseOrderAsync(productId, vendorId, quantity, deliveryDate);
                return Ok(new { POId = id, Message = "Purchase Order Created Successfully" });
            }
            catch (Exception ex)
            {

                return BadRequest(new { Error = ex.Message });
            }
        }


        [HttpGet("{vendorId}/pending-orders")]
        public async Task<IActionResult> GetOrders(int vendorId)
        {
            return Ok(await _vendorRepo.GetPendingOrdersAsync(vendorId));
        }


        [HttpPost("respond-po")]
        public async Task<IActionResult> RespondToPO(int poId, string status) // status = 'Accepted' or 'Rejected'
        {
            var result = await _vendorRepo.UpdatePOStatusAsync(poId, status);
            return result ? Ok("Status Updated") : BadRequest("Failed to update status");
        }


        [HttpGet("{vendorId}/dashboard")]
        public async Task<IActionResult> GetDashboard(int vendorId)
        {
            var data = await _vendorRepo.GetVendorDashboardAsync(vendorId);
            return Ok(data);
        }


        [HttpPatch("update-inventory")]
        public async Task<IActionResult> UpdateInventory(int productId, decimal price, int stock)
        {
            var success = await _vendorRepo.UpdateInventoryAsync(productId, price, stock);
            return success ? Ok("Inventory Updated") : BadRequest("Failed to update");
        }


        [HttpGet("{vendorId}/order-history")]
        public async Task<IActionResult> GetHistory(int vendorId)
        {
            return Ok(await _vendorRepo.GetOrderHistoryAsync(vendorId));
        }


        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] ProductRequest request, [FromQuery] int vendorId)
        {
            try
            {
                var id = await _vendorRepo.AddProductAsync(request, vendorId);
                return Ok(new { ProductId = id, Message = "Product added successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("update-product/{productId}")]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] ProductRequest request, [FromQuery] int vendorId)
        {
            var success = await _vendorRepo.UpdateProductAsync(productId, request, vendorId);
            return success ? Ok("Product updated successfully") : NotFound("Product not found or unauthorized");
        }

        [HttpGet("{vendorId}/my-products")]
        public async Task<IActionResult> GetMyProducts(int vendorId)
        {
            var products = await _vendorRepo.GetMyProductsAsync(vendorId);
            return Ok(products);
        }


        [HttpGet("{vendorId}/orders")]
        public async Task<IActionResult> GetOrders(int vendorId, [FromQuery] string status = "Pending")
        {


            var orders = await _vendorRepo.GetOrdersByStatusAsync(vendorId, status);
            return Ok(orders);
        }

        [HttpPatch("order/{poId}/update-status")]
        public async Task<IActionResult> UpdateStatus(int poId, [FromQuery] string status, [FromQuery] int vendorId)
        {
         
            var result = await _vendorRepo.UpdateOrderStatusAsync(poId, status, vendorId);
            return result ? Ok(new { Message = $"Order status updated to {status}" }) : BadRequest("Failed to update status");
        }

        [HttpGet("{vendorId}/wallet")]
        public async Task<IActionResult> GetWallet(int vendorId)
        {
            var walletData = await _vendorRepo.GetVendorWalletAsync(vendorId);
            return Ok(walletData);
        }


        [HttpGet("{vendorId}/dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats(int vendorId)
        {
            var stats = await _vendorRepo.GetVendorDashboardStatsAsync(vendorId);
            return stats != null ? Ok(stats) : NotFound("Stats not available");
        }
    }
}
