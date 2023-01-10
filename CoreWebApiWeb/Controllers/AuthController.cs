using CoreWebApiWeb.Models;
using CoreWebApiWeb.Services.UsersService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CoreWebApiWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static User user = new User();
        private readonly IConfiguration configuration;
        private readonly IUserService userService;

        public AuthController(IConfiguration configuration, IUserService userService) {
            this.configuration = configuration;
            this.userService = userService;
        }
        
        [HttpGet, Authorize]
        public ActionResult<string> GetMe()
        {
            var name = userService.GetName();
            return Ok(name);

/*            var claim = User.FindFirstValue(ClaimTypes.Role);
            return Ok(new { name, claim });
*/        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDTO dto)
        {
            user.Name = dto.Name;
            CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO dto)
        {
            if (user.Name != dto.Name)
            {
                return BadRequest("User not found.");
            }
            if (!VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("wrong pass!");
            }
            string tok = CreateToken(user);
            return Ok(tok);
        }

        [NonAction]
        private void CreatePasswordHash(string pass, out byte[] hash, out byte[] salt)
        {
            using (var hmac = new HMACSHA512())
            {
                salt = hmac.Key;
                hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pass));
            }
        }
        bool VerifyPasswordHash(string pass, byte[] hash, byte[] salt)
        {
            using (var hmac = new HMACSHA512(salt))
            {
                var chash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pass));
                return chash.SequenceEqual(hash);
            }
        }

        string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim> { 
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                configuration.GetSection("SToken:tok").Value));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
