using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressRepository _addressRepository;

        public AddressController(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddAddress([FromBody] UserAddress address)
        {
         
            address.Id = Guid.NewGuid();

            var result = await _addressRepository.AddAddressAsync(address);
            return result ? Ok(new { Message = "Address Saved!" }) : BadRequest();
        }

        [HttpGet("my-addresses/{userId}")]
        public async Task<IActionResult> GetMyAddresses(int userId)
        {
    
            var addresses = await _addressRepository.GetUserAddressesAsync(userId);
            return Ok(addresses);
        }


        [HttpDelete("{id}/{userId}")]
        public async Task<IActionResult> DeleteAddress(Guid id, int userId)
        {
            var result = await _addressRepository.DeleteAddressAsync(userId, id);
            return result ? Ok(new { Message = "Address Deleted" }) : NotFound();
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> GetLocationSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query)) return BadRequest("Query is empty");


            var dummySuggestions = new List<string> {
        "Satellite, Ahmedabad",
        "Prahlad Nagar, Ahmedabad",
        "Bopal, Ahmedabad"
    };

            return Ok(new { suggestions = dummySuggestions });
        }


    }
}
