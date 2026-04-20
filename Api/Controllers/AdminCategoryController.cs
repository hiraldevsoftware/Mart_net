using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/admin/categories")]
    public class AdminCategoryController : ControllerBase
    {
        private readonly IAdminCategoryRepository _categoryRepo;

        public AdminCategoryController(IAdminCategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryRepo.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category request)
        {
            var id = await _categoryRepo.AddCategoryAsync(request);
            return Ok(new { CategoryId = id, Message = "Category created successfully!" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Category request)
        {

            request.Id = id;

            var result = await _categoryRepo.UpdateCategoryAsync(request);

            if (result)
                return Ok(new { Message = "Category updated successfully!" });

            return NotFound(new { Message = "Category not found or update failed." });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _categoryRepo.DeleteCategoryAsync(id);

            if (result)
                return Ok(new { Message = "Category deactivated successfully!" });

            return NotFound(new { Message = "Category not found." });
        }


    }
}
