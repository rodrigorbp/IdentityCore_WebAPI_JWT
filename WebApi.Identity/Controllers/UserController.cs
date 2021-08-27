using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebApi.Dominio;
using WebApi.Identity.Dto;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApi.Identity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IMapper _mapper;

        public UserController(IConfiguration config, UserManager<User> userManager,
            SignInManager<User> signInManager, IMapper mapper)
        {
            _config = config;
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
        }


        // GET: api/<UserController>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new UserDto());
        }

        // GET api/<UserController>/5
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userLoginDto.UserName);

                var result = await _signInManager.CheckPasswordSignInAsync(user, userLoginDto.Password, false);

                if (result.Succeeded)
                {
                    var appUser = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.NormalizedUserName == user.UserName.ToUpper());


                    var userToReturn = _mapper.Map<UserDto>(appUser);

                    return Ok( new 
                    { 
                        token = GenerateJWToken(appUser).Result,
                        user = userToReturn
                    });
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Error {ex.Message}");
            }
        }

        // POST api/<UserController>
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(userDto.UserName);

                if (user == null)
                {
                    user = new User
                    {
                        UserName = userDto.UserName,
                        Email = userDto.UserName,
                        NomeCompleto = userDto.NomeCompleto
                    };

                    var result = await _userManager.CreateAsync(user, userDto.Password);

                    if (result.Succeeded)
                    {
                        var appUser = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.NormalizedUserName == user.UserName.ToUpper());

                        var token = GenerateJWToken(appUser).Result;
                        //var confirmationEmail = Url.Action("ConfirmEmailaddress", "Home", new { token = token, email = user.Email }, Request.Scheme);
                        //System.IO.File.WriteAllText(@"D:\Cursos\confirmationEmail.txt", confirmationEmail);
                        return Ok(token);
                    }
                }
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Error {ex.Message}");
            }
        }

        private async Task<string> GenerateJWToken(User user)
        {
            //criar credenciais
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            //pega as roles que estão para o usuário
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            //criar uma chave de criptografia com a chave que está no arquivo de configuração
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value));

            //criar credencial com a assinatura que será utilizada
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            //Gera a descrição do token
            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            //Gerar o token
            var tokenHandler = new JwtSecurityTokenHandler();

            //Criar o token
            var token = tokenHandler.CreateToken(tokenDescription);

            //retornar o token
            return tokenHandler.WriteToken(token);

        }
        //[HttpPost]
        //public void Post(UserDto model)
        //{
        //}

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UserController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
