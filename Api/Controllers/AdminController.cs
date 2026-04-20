
using Mart.Api.Models;
using Mart.Api.Models.JPMart.Models.DTOs;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Mart.Persistence;
using Mart.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Mart.Api.Controllers
{
    //[Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminProductRepository _adminRepo;
        private readonly IMediaService _mediaService;
        private readonly IQRCodeService _qrService;
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminController(IAdminProductRepository adminRepo, IMediaService mediaService, IQRCodeService qrServices, IDbConnectionFactory connectionFactory)
        {
            _adminRepo = adminRepo;
            _mediaService = mediaService;
            _qrService = qrServices;
            _connectionFactory = connectionFactory;
        }


        [HttpPost("products")]
        public async Task<IActionResult> AddProduct([FromBody] ProductRequest product)
        {
            var id = await _adminRepo.AddProductAsync(product);
            return Ok(new { ProductId = id, Message = "Product added successfully!" });
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductRequest product)
        {
            if (id != product.Id) return BadRequest("ID mismatch");

            var result = await _adminRepo.UpdateProductAsync(product);
            return result ? Ok("Product updated successfully!") : NotFound();
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _adminRepo.DeleteProductAsync(id);
            return result ? Ok("Product deleted successfully!") : NotFound();
        }

        [HttpPost("products/{id}/media")]
        public async Task<IActionResult> UploadMedia(int id, IFormFile file, [FromQuery] bool isPrimary = false)
        {
    
            var imageUrl = await _mediaService.UploadImageAsync(file, "products");

            if (imageUrl == null) return BadRequest("Image upload failed!");


            var media = new ProductMedia
            {
                ProductId = id,
                MediaUrl = imageUrl,
                MediaType = "image",
                DisplayOrder = 1,
                IsPrimary = isPrimary
            };

            var result = await _adminRepo.AddProductMediaAsync(media);

            return Ok(new { Url = imageUrl, Success = result });
        }


        [HttpPatch("products/{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromQuery] int storeId, [FromQuery] int quantity)
        {
            var result = await _adminRepo.UpdateStockAsync(id, storeId, quantity);
            return result ? Ok("Stock Updated & Redis Synced!") : BadRequest("Failed!");
        }

        [HttpPost("sync-redis/{storeId}")]
        public async Task<IActionResult> SyncRedis(int storeId)
        {
            var result = await _adminRepo.BulkSyncToRedisAsync(storeId);
            return result ? Ok($"Redis synced for store {storeId}") : BadRequest();
        }

        [HttpPost("variants")]
        public async Task<IActionResult> AddVariant([FromBody] ProductVariantRequest request)
        {
            var result = await _adminRepo.AddVariantAsync(request);
            return result ? Ok("Variant added!") : BadRequest();
        }


        [HttpGet("{productId}/variants")]
        public async Task<IActionResult> GetVariants(int productId) => Ok(await _adminRepo.GetVariantsByProductIdAsync(productId));


        [HttpGet("expiry-alerts")]
        public async Task<IActionResult> GetExpiryAlerts([FromQuery] int days = 7) => Ok(await _adminRepo.GetExpiryAlertsAsync(days));

        [HttpPatch("batch/{id}/mark-waste")]
        public async Task<IActionResult> MarkWaste(int id) => Ok(await _adminRepo.MarkBatchAsWasteAsync(id));



        [HttpGet("scan/{barcode}")]
        public async Task<IActionResult> GetByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return BadRequest(new { Message = "Barcode cannot be empty" });
            }

            string decodedBarcode = System.Net.WebUtility.UrlDecode(barcode);
            string finalSearchValue = decodedBarcode;

          
            if (decodedBarcode.Trim().StartsWith("{"))
            {
                try
                {
                    using (var jsonDoc = System.Text.Json.JsonDocument.Parse(decodedBarcode))
                    {
                        if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
                        {
                            finalSearchValue = idElement.ToString();
                        }
                    }
                }
                catch
                {
                    finalSearchValue = decodedBarcode;
                }
            }

            var product = await _adminRepo.GetProductByBarcodeAsync(finalSearchValue);

            if (product == null)
            {
                return NotFound(new { Message = $"Product with barcode '{finalSearchValue}' not found" });
            }

       
            var response = new
            {
                id = product.Id,
                name = product.Name,
                description = product.Description ?? "",
                price = new
                {
                    amount = product.Price != null ? product.Price.Amount : 0,
                    currency = product.Price != null ? product.Price.Currency : "INR"
                },
                stockCount = product.StockCount,
                minStockAlert = product.MinStockAlert, 
                categoryId = product.CategoryId,
                barcode = product.Barcode,
                isDeleted = product.IsDeleted
            };

            return Ok(response);
        }



        [HttpGet("{productId}/qr")]
        public async Task<IActionResult> GetProductQR(int productId)
        {
            var product = await _adminRepo.GetProductByIdAsync(productId);
            if (product == null) return NotFound();

            string qrData = $"{{ \"type\": \"product\", \"id\": {productId} }}";
            string base64Qr = _qrService.GenerateQRCodeBase64(qrData);


            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string updateSql = "UPDATE Products SET QRCodeBase64 = @QR WHERE Id = @Id";

            using var command = new SqlCommand(updateSql, connection);
            command.Parameters.AddWithValue("@QR", base64Qr);
            command.Parameters.AddWithValue("@Id", productId);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return Ok(new { ProductId = productId, QRCodeBase64 = base64Qr });
        }



        [HttpGet("inventory-alerts")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            try
            {
                // રેપોઝીટરીમાંથી એલર્ટ્સ મેળવો
                var alerts = await _adminRepo.GetLowStockAlertsAsync();

                // જો લિસ્ટ ખાલી હોય તો પણ ખાલી લિસ્ટ (Empty Array) મોકલીશું
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                // એરર હેન્ડલિંગ
                return StatusCode(500, new { Message = "Alerts મેળવવામાં તકલીફ થઈ!", Error = ex.Message });
            }
        }




        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var summary = await _adminRepo.GetDashboardSummaryAsync();
            return Ok(summary);
        }



        [HttpPost("bulk-price-update")]
        public async Task<IActionResult> BulkPriceUpdate([FromBody] BulkUpdateDto dto)
        {

            var affectedRows = await _adminRepo.BulkUpdatePriceAsync(dto.ProductIds, dto.Percentage);
            return Ok(new { Message = $"{affectedRows} Product Price Updated" });
        }

    }
}
