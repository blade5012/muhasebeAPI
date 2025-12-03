using System;
using System.Security.Cryptography;
using System.Text;

namespace MuhasebeAPI.Helpers
{
    public static class HashingHelper
    {
        // 1️⃣ Rastgele salt üret
        public static string GenerateSalt(int size = 16)
        {
            byte[] saltBytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // 2️⃣ Şifreyi hashle (salt ile)
        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(salt)) throw new ArgumentNullException(nameof(salt));

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // 3️⃣ Şifre doğrulama
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            string hashOfInput = HashPassword(password, storedSalt);
            return hashOfInput == storedHash;
        }
    }
}
