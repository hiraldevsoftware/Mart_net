using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [Route("api/admin/support")]
    [ApiController]
    public class AdminSupportController : ControllerBase
    {
        private readonly IAdminSupportRepository _supportRepo;
        public AdminSupportController(IAdminSupportRepository repo) => _supportRepo = repo;


        [HttpPost("maintenance")]
        public async Task<IActionResult> ToggleMaintenance([FromBody] bool isOn)
        {
            await _supportRepo.SetMaintenanceModeAsync(isOn);
            return Ok(new { Message = isOn ? "App is now in Maintenance Mode" : "App is now Live" });
        }


        [HttpPatch("tax/{id}")]
        public async Task<IActionResult> UpdateTax(int id, [FromBody] decimal rate)
        {
            var result = await _supportRepo.UpdateTaxRateAsync(id, rate);
            return result ? Ok("Tax rate updated!") : NotFound();
        }
    }
}
