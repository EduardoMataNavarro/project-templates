using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_asp.Configurations;
using backend_asp.Models.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// TODO: run "dotnet ef migrations add "initial-migration" && dotnet ef database update to create migrations for the auth module"
namespace backend_asp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> logger;
        private readonly UserManager<IdentityUser> userManager;
        private readonly JwtConfig jwtConfig;


        public AuthController(
            ILogger<AuthController> _logger,
            UserManager<IdentityUser> _userManager,
            IOptionsMonitor<JwtConfig> _optionsMonitor
        )
        {
            logger = _logger;
            userManager = _userManager;
            jwtConfig = _optionsMonitor.CurrentValue;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto req)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await userManager.FindByEmailAsync(req.Email);
                if (existingUser != null)
                    return Conflict("A user with this email alredy exists");

                var newUser = new IdentityUser()
                {
                    Email = req.Email,
                    UserName = req.Username,
                };

                var savedUser = await userManager.CreateAsync(newUser, req.Password);
                if (savedUser.Succeeded)
                    return Ok(new AuthResultDto()
                    {
                        Success = true,
                        Message = new List<string>() {
                            "Successfuly created user",
                            "Please Check Your Email to confirm your account",
                        },
                        Token = GenerateAuthToken(newUser),
                    });

                return BadRequest(savedUser.Errors.Select(err => err.Description).ToList());

            }
            return BadRequest("Please input valid values");

        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto req)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await userManager.FindByEmailAsync(req.Email);
                if (existingUser == null)
                    return BadRequest("Please register instead");
                var isPasswordValid = await userManager.CheckPasswordAsync(existingUser, req.Password);
                logger.Log(LogLevel.Information, $"Password {isPasswordValid}");

                if (isPasswordValid)
                    return Ok(new LoginResponseDto()
                    {
                        Success = true,
                        Message = new List<string>() { "Successfuly logged in" },
                        Token = GenerateAuthToken(existingUser),
                    });


                return Unauthorized();
            }
            return BadRequest("Please input valid values");
        }

        private string GenerateAuthToken(IdentityUser user)
        {

            var jwtHandler = new JwtSecurityTokenHandler();
            var secret = jwtConfig.Secret;
            var key = Encoding.ASCII.GetBytes(secret);
            
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = jwtHandler.CreateToken(tokenDescriptor);
            var jwt = jwtHandler.WriteToken(token);
            return jwt;
        }
    }
}