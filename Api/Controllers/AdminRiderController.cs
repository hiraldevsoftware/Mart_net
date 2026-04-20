using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/admin/riders")]
    public class AdminRiderController: ControllerBase
    {
        private readonly IAdminRiderRepository _riderRepo;

        public AdminRiderController(IAdminRiderRepository riderRepo)
        {
            _riderRepo = riderRepo;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _riderRepo.GetAllRidersAsync());


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RiderRequest request)
        {
            var id = await _riderRepo.CreateRiderAsync(request);
            return Ok(new { RiderId = id, Message = "Rider Created!" });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] bool isAvailable)
        {
            var result = await _riderRepo.UpdateRiderStatusAsync(id, isAvailable);
            return result ? Ok("Status Updated!") : NotFound();
        }

        [HttpGet("{id}/wallet")]
        public async Task<IActionResult> GetWallet(int id) => Ok(await _riderRepo.GetRiderWalletAsync(id));


        [HttpPost("{id}/settle")]
        public async Task<IActionResult> SettlePayment(int id, [FromBody] decimal amount)
        {
            var result = await _riderRepo.SettleRiderPayoutAsync(id, amount);
            return result ? Ok("Payment settled successfully!") : BadRequest("Insufficient pending balance.");
        }

        [HttpPost("{id}/shift")]
        public async Task<IActionResult> UpdateShift(int id, string type, string start, string end)
        {
            var startTime = TimeSpan.Parse(start);
            var endTime = TimeSpan.Parse(end);
            var result = await _riderRepo.AssignShiftAsync(id, type, startTime, endTime);
            return result ? Ok("Shift updated!") : BadRequest();
        }


        [HttpGet("{id}/shifts")]
        public async Task<IActionResult> GetShifts(int id)
        {
            var shifts = await _riderRepo.GetRiderShiftsAsync(id);
            return Ok(shifts);
        }


        [HttpPost("{orderId}/assign-request/{riderId}")]
        public async Task<IActionResult> SendRequest(int orderId, int riderId)
        {

            var result = await _riderRepo.SendAssignmentRequestAsync(orderId, riderId);

            if (result)
            {
          
                return Ok(new
                {
                    Success = true,
                    Message = "Rider ne notification mokli didhi che. 60 seconds ma reply aavshe."
                });
            }

            return BadRequest("Request moklavanya fail thai.");
        }


        [HttpPost("{riderId}/approve")]
        public async Task<IActionResult> ApproveRider(int riderId)
        {
            var result = await _riderRepo.ApproveRiderAsync(riderId);
            if (result) return Ok(new { message = "Rider approved successfully!" });
            return BadRequest("Rider approval failed or rider not found.");
        }

    
        [HttpGet("live-locations")]
        public async Task<IActionResult> GetLiveLocations()
        {
            var locations = await _riderRepo.GetLiveRiderLocationsAsync();
            return Ok(locations);
        }

  
        [HttpPost("{riderId}/settle-cash")]
        public async Task<IActionResult> SettleCash(int riderId)
        {
            var result = await _riderRepo.SettleRiderCashAsync(riderId);
            if (result) return Ok(new { message = "Cash settled and wallet reset." });
            return BadRequest("Cash settlement failed.");
        }


        [HttpPost("{riderId}/payout-done")]
        public async Task<IActionResult> MarkAsPaid(int riderId)
        {
            var result = await _riderRepo.MarkPayoutAsPaidAsync(riderId);
            if (result) return Ok(new { message = "Payout marked as paid successfully." });
            return BadRequest("Payout update failed.");
        }
    }
}
