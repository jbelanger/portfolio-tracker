using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Portfolio.App.Services;
using Portfolio.Infrastructure.Identity;

namespace Portfolio.Api.Features
{
    public class RegisterGoogleRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string IdToken { get; set; }
    }


    public static class AuthEndpoints
    {
        public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/auth");

            group.MapPost("/google-signin", async (
                [FromBody] RegisterGoogleRequest request,
                UserManager<ApplicationUser> userManager,
                IConfiguration configuration) =>
            {
                var payload = await VerifyGoogleToken(request.IdToken, configuration["Authentication:Google:ClientId"]);

                if (payload == null)
                {
                    return Results.Unauthorized();
                }
try{
                var user = await userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        GoogleId = payload.Email
                    };
                    var result = await userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return Results.BadRequest("User creation failed.");
                    }
                }

                var token = GenerateJwtToken(user, configuration);
                return Results.Ok(new { Token = token });
}
catch(Exception ex)
{
    return Results.Problem(ex.Message);
}
            });

            group.MapPost("/register-google", async ([FromBody] RegisterGoogleRequest request, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user != null)
                {
                    return Results.Ok(); // User already exists, return success
                }

                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    //GoogleId = request.GoogleId // Assuming you've extended ApplicationUser with a GoogleId property
                };

                var result = await userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    return Results.Ok();
                }

                return Results.BadRequest(result.Errors);
            });
        }

        static async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string tokenId, string clientId)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { clientId }
            };

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(tokenId, settings);
                return payload;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string GenerateJwtToken(IdentityUser user, IConfiguration configuration)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public record GoogleSignInRequest(string TokenId);
    }
}