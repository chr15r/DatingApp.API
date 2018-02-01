using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _repo; 
        private readonly IConfiguration _config; 
        public AuthController(IAuthRepository repo, IConfiguration config) {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDTO userForRegisterDTO){  

            userForRegisterDTO.Username = userForRegisterDTO.Username.ToLower();
            if(await _repo.UserExists(userForRegisterDTO.Username)) {
                ModelState.AddModelError("Username","Username already exists");
            }

            // Validate Request
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);       
                
            var userToCreate = new User {
                Username = userForRegisterDTO.Username
            };

            var createUser = await _repo.Register(userToCreate, userForRegisterDTO.Password);   

            return StatusCode(201); 
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForLoginDTO userForLoginDTO) {
            var userFromRepo = await _repo.Login(userForLoginDTO.Username.ToLower(), userForLoginDTO.Password);
            if (userFromRepo == null) 
                return Unauthorized();

            // generate token
            var tokenhandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value);
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.Username)
                }),
                Expires = System.DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha512Signature)
            };
            
            try
            {
                var token = tokenhandler.CreateToken(tokenDescriptor);
                var tokenString = tokenhandler.WriteToken(token);
                return Ok(new {tokenString});
            }
            catch (System.Exception e)
            {
                var s = e.Message;
                return Unauthorized();
                throw;
            }
           
        }
        
    }
}