using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ProductController: ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly IQRCodeService _qrService;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
 
        }



        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productRepository.GetAllProductsAsync();

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price.Amount, 
                Currency = p.Price.Currency,
                StockCount = p.StockCount,
                CategoryId = p.CategoryId,
                Variants = p.Variants,
                ImageUrl = p.ImageUrl

            });

            return Ok(response);
        }



        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult>GetByCategory(Guid categoryId)
        {
            var products = await _productRepository.GetProductsByCategoryAsync(categoryId);

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                StockCount = p.StockCount,
                CategoryId = p.CategoryId
            });

            return Ok(response);
        }



 


        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
       
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest(new { Message = "Search term cannot be empty" });
            }

            var products = await _productRepository.SearchProductsAsync(term);

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                StockCount = p.StockCount,
                CategoryId = p.CategoryId,

                 //Tags = p.Tags
            });

            return Ok(response);
        }

        //[HttpPost("add-to-stock")]
        //public async Task<IActionResult> AddToStock([FromBody] CartRequest request)
        //{

        //    int availableStock = await _productRepository.GetStockAsync(request.ProductId, request.StoreId);

        //    if (availableStock <= 0)
        //    {
        //        return BadRequest(new
        //        {
        //            Message = "Item Is Out Of Stock",
        //            ShowNotifyMe = true
        //        });
        //    }

        //    if (availableStock < request.Quantity)
        //    {
        //        return BadRequest(new { Message = $"only {availableStock} items remaining" });
        //    }


        //    return Ok(new { Message = "Item added to cart successfully" });
        //}



        [HttpPost("add-to-stock")]
        public async Task<IActionResult> AddToStock([FromBody] CartRequest request)
        {
            try
            {
                var result = await _productRepository.AddToCartAsync(request.UserId, request.ProductId, request.Quantity, request.StoreId);

                if (result == "Insufficient stock!") return BadRequest(new { message = result });
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
               
                return BadRequest(new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }



        [HttpPost("update-stock")]
        public async Task<IActionResult> UpdateStock(int productId, int storeId, int quantityChange)
        {

            bool isUpdated = await _productRepository.UpdateStockAsync(productId, storeId, quantityChange);

            if (!isUpdated)
            {
                return BadRequest(new
                {
                    message = "Stock is not update."
                });
            }

     
            int newStock = await _productRepository.GetStockAsync(productId, storeId);

            return Ok(new
            {
                message = "Stock updated successfully!",
                currentStock = newStock
            });
        }



        [HttpPost("wishlist")]
        public async Task<IActionResult> AddToWishlist(int userId, int productId)
        {
            var result = await _productRepository.AddToWishlistAsync(userId, productId);
            return result ? Ok("Added to wishlist") : BadRequest("Already in wishlist or error");
        }


        [HttpGet("wishlist/{userId}")]
        public async Task<IActionResult> GetWishlist(int userId)
        {
            var products = await _productRepository.GetWishlistAsync(userId);
            return Ok(products);
        }


    

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CartRequest request)
        {
            var result = await _productRepository.AddToCartAsync(
                request.UserId,
                request.ProductId,
                request.Quantity,
                request.StoreId
            );

            if (result == "Success") return Ok(new { message = "Item added to SQL cart!" });
            return BadRequest(new { message = result });
        }




        [HttpGet("suggestions/{productId}")]
        public async Task<IActionResult> GetSuggestions(int productId)
        {
            try
            {
                var suggestions = await _productRepository.GetFrequentlyBoughtTogetherAsync(productId);

                if (suggestions == null || !suggestions.Any())
                {
             
                    return Ok(new { message = "No specific suggestions", data = new List<object>() });
                }

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error fetching suggestions", details = ex.Message });
            }
        }



        [HttpGet("{productId}/media")]
        public async Task<IActionResult> GetMedia(int productId)
        {
            var media = await _productRepository.GetProductMediaAsync(productId);

            if (media == null || !media.Any())
            {
                return NotFound(new { message = "This Product Have Nothing Media." });
            }

            return Ok(media);
        }




        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFromCart([FromBody] CartRequest request)
        {
            try
            {

                var result = await _productRepository.RemoveFromCartAsync(
                    request.UserId,
                    request.ProductId,
                    request.Quantity,
                    request.StoreId
                );

                if (result == "Success")
                {
                    return Ok(new { message = "Item removed from cart successfully!" });
                }
                else if (result == "Item not in cart")
                {
                    return BadRequest(new { message = result });
                }
                else
                {
                    return BadRequest(new { message = "Failed to remove item" });
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"REMOVE ERROR: {ex.Message}");
                return BadRequest(new { message = "Something went wrong!" });
            }
        }

 



       

    }
}
