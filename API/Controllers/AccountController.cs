using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        public ITokenService _tokenService { get; }
        public AccountController(DataContext context, ITokenService tokenService){
            _tokenService = tokenService;
            _context = context;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDTO) {
         
            if (await UserExist(registerDTO.Username)) return BadRequest("Username is already taken!");

         using var hmac = new HMACSHA512();
         
         var user = new AppUser
         {
            UserName = registerDTO.Username,
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
            PasswordSalt = hmac.Key
         };

         _context.Users.Add(user);
         await _context.SaveChangesAsync();

         return new UserDto
         {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user)
         };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users
            .SingleOrDefaultAsync(x => x.UserName== loginDTO.Username);

            if (user == null) return Unauthorized("Invalid Username!");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }

             return new UserDto
         {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user)
         };


        }
        private async Task<bool> UserExist(string username)
        {
          return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}