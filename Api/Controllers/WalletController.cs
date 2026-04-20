using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController: ControllerBase
    {
        private readonly IWalletRepository _repo;
        public WalletController(IWalletRepository repo) => _repo = repo;

        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetBalance(int userId)
        {
            var balance = await _repo.GetWalletBalanceAsync(userId);
            return Ok(new { userId, balance });
        }


        [HttpPost("add-money")]
        public async Task<IActionResult> AddMoney([FromBody] WalletAddRequest request)
        {


            await _repo.UpdateWalletBalanceAsync(request.UserId, request.Amount);

            return Ok(new { message = $"Rs. {request.Amount} wallet ma add thai gaya!", newBalance = "Check Balance API" });
        }
    }
}
