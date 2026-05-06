using Mart.Api.Models;
using Mart.Domain.Interface;
using Mart.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminStoreController : ControllerBase
    {
        private readonly IAdminStoreRepository _adminRepo;
        private readonly IStoreRepository _storeRepo;
        private readonly IDbConnectionFactory _connectionFactory;
        public AdminStoreController(IAdminStoreRepository adminRepo, IStoreRepository storeRepo, IDbConnectionFactory connectionFactory) 
            {
                _adminRepo = adminRepo;
                _storeRepo = storeRepo;
               _connectionFactory = connectionFactory;
            }

        

        [HttpGet("pending-requests")]
        public async Task<IActionResult> GetRequests() => Ok(await _adminRepo.GetPendingStockRequestsAsync());

        [HttpPost("process-stock-request")]
        public async Task<IActionResult> ProcessRequest([FromBody] StockApprovalDto dto)
        {
            try
            {
                var result = await _adminRepo.ProcessStockRequestAsync(dto);
                return result ? Ok("Stock request processed") : BadRequest("Failed to process");
            }
            catch (Exception ex)
            {
   
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("audit-reports")]
        public async Task<IActionResult> GetAudits() => Ok(await _adminRepo.GetInventoryAuditReportsAsync());

        [HttpGet("all-stores")]
        public async Task<IActionResult> GetAllStores()
        {
            try
            {
       
                var stores = await _storeRepo.GetAllStoresAsync();


                var result = stores.Select(s => new {
                    Id = s.Id,
                    StoreName = s.StoreName
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }



        [HttpGet("assignable-users")]
        public async Task<IActionResult> GetAssignableUsers()
        {
            var users = await _adminRepo.GetAssignableUsersAsync();
            return Ok(users);
        }


        //[HttpPost("assign-manager")]
        //public async Task<IActionResult> AssignManager([FromBody] AssignManagerRequest request)
        //{
        //    if (request == null || request.UserId <= 0 || request.StoreId <= 0)
        //        return BadRequest("Invalid Data");

        //    var result = await _adminRepo.AssignManagerToStoreAsync(request.UserId, request.StoreId);

        //    if (result)
        //        return Ok(new { message = "Manager assigned successfully!" });

        //    return StatusCode(500, "Update failed.");
        //}


        [HttpPost("assign-order-to-store")]
        public async Task<IActionResult> AssignOrderToStore(int orderId, int storeId)
        {
       
            var result = await _adminRepo.AssignOrderToStoreAsync(orderId, storeId);

            if (result)
                return Ok(new { message = "Order successfully assigned to store manager!" });

            return BadRequest("Failed to assign order.");
        }



    }
}
