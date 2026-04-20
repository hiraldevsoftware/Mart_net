using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Mart.Persistence.Repositories;
using Mart.Persistence.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtRepository;

        public AuthController(IUserRepository userRepository, IJwtService jwtRepository)
        {
            _userRepository = userRepository;
            _jwtRepository = jwtRepository;

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);

            if (user == null)
            {
                return NotFound(new { Message = "User not found. Please register first." });
            }

            var token = _jwtRepository.GenerateToken(user);

            return Ok(new
            {
                Message = "Login Successful",
                Token = token,
                User = user
            });
        }




        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            
            var existingUser = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "Already Created account on this Number. Login Again" });
            }

           
            var newUser = new User
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,           
                IsPhoneVerified = false,        
                WalletBalance = 0,              
                CreatedAt = DateTime.UtcNow,   
                IsDeleted = false,
                OtpCode = "123456", 
                OtpExpiry = DateTime.UtcNow.AddMinutes(5)
            };

          
            var userId = await _userRepository.CreateUserAsync(newUser);
            newUser.Id = userId;

            
            var token = _jwtRepository.GenerateToken(newUser);

            return Ok(new
            {
                Message = "Registration Successful!",
                Token = token,
                User = newUser
            });
        }




        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var user = await _userRepository.GetUserByPhoneAsync(request.PhoneNumber);
            if (user == null) return NotFound("User Not Found");


            Console.WriteLine($"DB OTP: '{user.OtpCode}' | Input OTP: '{request.Otp}'");

  
            if (user.OtpCode == request.Otp)
            {
                user.IsPhoneVerified = true;
                user.OtpCode = null; 
                user.OtpExpiry = null;

                await _userRepository.UpdateUserAsync(user);

                var token = _jwtRepository.GenerateToken(user);

                return Ok(new
                {
                    Message = "Phone verified successfully (No Expiry Check)!",
                    Token = token
                });
            }

            return BadRequest(new { Message = "OTP is wrong." });
        }
    }
}
