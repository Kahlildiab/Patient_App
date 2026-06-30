using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DentalCollegeManagementSystem_AAU.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(
            int userId,
            string username,
            string givenName,
            string surname,
            string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.GivenName, givenName ?? string.Empty),
                new Claim(ClaimTypes.Surname, surname ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            string keyValue = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing.");
            string issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
            string audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience is missing.");
            int expiryHours = _configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 2;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            DateTime now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: now.AddHours(expiryHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
