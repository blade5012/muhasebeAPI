using System;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Configuration; // IConfiguration için
using System.Collections.Generic; // List için

namespace MuhasebeAPI.Helpers
{
    public static class TokenHelper
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GenerateToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes)
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "");
            }
        }

        public static string HashToken(string token)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                return Convert.ToBase64String(bytes);
            }
        }

        public static string GenerateJwtToken(int userId, string email, string role, string username) // username parametresi eklendi
        {
            Console.WriteLine("GenerateJwtToken çağrıldı.");
            var jwtSettings = _configuration.GetSection("JwtSettings");
            
            if (jwtSettings == null)
            {
                Console.WriteLine("Hata: JwtSettings bölümü null.");
                throw new InvalidOperationException("JwtSettings bölümü yapılandırılmamış.");
            }

            var secret = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            Console.WriteLine($"Secret: {secret}");
            Console.WriteLine($"Issuer: {issuer}");
            Console.WriteLine($"Audience: {audience}");

            if (string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("Hata: JwtSettings[\"Secret\"] boş veya null.");
                throw new InvalidOperationException("JwtSettings[\"Secret\"] yapılandırılmamış.");
            }

            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            // DisplayName claim'ini ekle
            string displayName = string.IsNullOrEmpty(username) ? email.Split('@')[0] : username;
            claims.Add(new Claim("displayName", displayName)); // Yeni displayName claim'i

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = issuer,
                Audience = audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
