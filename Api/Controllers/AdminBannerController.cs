using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/admin/banners")]
    public class AdminBannerController : ControllerBase
    {
        private readonly IAdminBannerRepository _bannerRepo;
        public AdminBannerController(IAdminBannerRepository bannerRepo) => _bannerRepo = bannerRepo;


        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _bannerRepo.GetAllBannersAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BannerRequest request)
        {
            if (string.IsNullOrEmpty(request.ImageUrl))
                return BadRequest("Image URL is required.");

            var id = await _bannerRepo.AddBannerAsync(request);
            return Ok(new { BannerId = id, Message = "Banner Added Successfully!" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _bannerRepo.DeleteBannerAsync(id);
            return result ? Ok("Banner Deleted!") : NotFound();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] bool isActive)
        {
            var result = await _bannerRepo.ToggleBannerStatusAsync(id, isActive);
            return result ? Ok("Status Updated!") : NotFound();
        }
    }
}
