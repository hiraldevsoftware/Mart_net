using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [Route("api/admin/marketing")]
    [ApiController]
    public class AdminMarketingController : ControllerBase
    {
        private readonly IAdminMarketingRepository _marketingRepo;
        public AdminMarketingController(IAdminMarketingRepository repo) => _marketingRepo = repo;


        [HttpGet("referral-bonus")]
        public async Task<IActionResult> GetBonus() => Ok(await _marketingRepo.GetReferralBonusAsync());


        [HttpPost("referral-bonus")]
        public async Task<IActionResult> UpdateBonus([FromBody] decimal amount)
        {
            var result = await _marketingRepo.UpdateReferralBonusAsync(amount);
            return result ? Ok("Referral bonus updated!") : BadRequest();
        }

        [HttpGet("top-referrers")]
        public async Task<IActionResult> GetTopReferrers() => Ok(await _marketingRepo.GetTopReferrersAsync());
    }
}
