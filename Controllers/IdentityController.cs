using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using identity.Entities.Identities;
using identity.Resources;
using identity.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace identity.Controllers
{
    [Route("api/[controller]")]
    public class IdentityController : Controller
    {
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly JwtSettings _jwtSettings;

        public IdentityController(IMapper mapper, UserManager<User> userManager, RoleManager<Role> roleManager, IOptionsSnapshot<JwtSettings> jwtSettings)
        {
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;

            // Basically we have create an object, JwtSettings, that maps out the "Jwt" key and its values specified in the appsettings.json file
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp(UserSignUpResource userSignUpResource)
        {

            var user = _mapper.Map<UserSignUpResource, User>(userSignUpResource);

            var userCreatedResults = await _userManager.CreateAsync(user, userSignUpResource.Password);

            if (userCreatedResults.Succeeded)
            {
                return Created(string.Empty, string.Empty);
            }

            return Problem(userCreatedResults.Errors.First().Description, null, 500);
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            var user = _userManager.Users.SingleOrDefault(u => u.UserName == email);

            if (user is null)
            {
                return NotFound("User not found");
            }

            var userSignInResult = await _userManager.CheckPasswordAsync(user, password);

            if (userSignInResult)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var jwt = GenerateJwt(user, roles);
                return Ok(jwt);
            }

            return BadRequest("Email or password is incorrect");
        }

        [HttpPost("Roles")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return BadRequest("Role name should be provided.");
            }

            var newRole = new Role() { Name = roleName };

            var roleResult = await _roleManager.CreateAsync(newRole);

            if (roleResult.Succeeded)
            {
                return Ok();
            }

            return Problem(roleResult.Errors.First().Description, null, 500);
        }

        // This will appear as a new record in the AspNetUserRoles table if this is successful
        [HttpPost("User/{userEmail}/Role")]
        public async Task<IActionResult> AddUserToRole(string userEmail, [FromBody] string roleName)
        {
            var user = _userManager.Users.SingleOrDefault(u => u.UserName == userEmail);

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                return Ok();
            }

            return Problem(result.Errors.First().Description, null, 500);
        }

        // Secret Test Path for Authorization
        [HttpGet("Secret")]
        [Authorize]
        public string GetSecret()
        {
            return "Hello World!";
        }

        // Method to generate a Json Web Token
        private string GenerateJwt(User user, IList<string> roles)
        {
            // These claims will appear in the newly generated Json Web Token
            var claims = new List<Claim>()
            {
               new Claim("id", user.Id.ToString())
            };

            // Iterate through the roles and add them to the claims
            var roleClaims = roles.Select(r => new Claim("roles", r));
            claims.AddRange(roleClaims);

            // Convert Secret Key into a byte array of UTF8 encoding
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            // Applies the HmacSha256 algorithim with the byte array representing the secret key
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Set the expiration date for the JWT
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_jwtSettings.ExpirationInDays));

            // Create a Jwt security token with optional parameters 
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Issuer,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            // Serializes the Json Web Token to a Compact Serialization Format
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
