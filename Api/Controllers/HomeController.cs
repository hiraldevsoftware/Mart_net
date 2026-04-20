using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController: ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public HomeController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }


        [HttpGet("layout")]
        public async Task<IActionResult> GetLayout([FromQuery]int userId)
        {
            var response = new HomeLayoutResponse();

            response.Sections.Add(
                new HomeSection
                {
                    SectionType = "Banners",
                    Title = "Today's Offers",
                    Data = new List<string> { "banner1_url", "banner2_url" }
                }
                );


            var categories = await _categoryRepository.GetAllCategoriesAsync();
            response.Sections.Add(new HomeSection
            {
                SectionType = "Categories",
                Title = "Shop by Category",
                Data = categories.Take(8)
            });


            var products = await _productRepository.GetAllProductsAsync();
            response.Sections.Add(new HomeSection
            {
                SectionType = "Products",
                Title = "Popular Near You",
                Data = products.Take(6)
            });

            return Ok(response);
        }
    }                                                                      
}
                                                                      