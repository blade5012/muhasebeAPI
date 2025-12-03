using Microsoft.AspNetCore.Mvc;
using MuhasebeAPI.Helpers;
using MuhasebeAPI.Models;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.Extensions.Options; // IOptions için

namespace MuhasebeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Baglanti _baglanti = new Baglanti();
        private readonly EmailHelper _emailHelper; // EmailHelper'ı enjekte et
        private readonly AppConfiguration _appConfig; // AppConfiguration enjekte et

        public AuthController(EmailHelper emailHelper, IOptions<AppConfiguration> appConfig)
        {
            _emailHelper = emailHelper;
            _appConfig = appConfig.Value;
        }

        // 1️⃣ Kullanıcı Kayıt
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "E-posta ve şifre zorunludur." });

            // Password hash ve salt
            string salt = HashingHelper.GenerateSalt();
            string hash = HashingHelper.HashPassword(model.Password, salt);

            int newUserId;
            string userToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            string adminToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            using (var conn = _baglanti.GetConnection())
            {
                await conn.OpenAsync();

                // Kullanıcıyı ekle
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO Users (Email, Username, PasswordHash, PasswordSalt, IsActive, IsEmailConfirmed)
            OUTPUT INSERTED.Id
            VALUES (@Email, @Username, @PasswordHash, @PasswordSalt, 0, 0)", conn);

                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Username", model.Username ?? "");
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@PasswordSalt", salt);

                newUserId = (int)await cmd.ExecuteScalarAsync();

                // Kullanıcı doğrulama tokeni ekle
                SqlCommand userTokenCmd = new SqlCommand(@"
            INSERT INTO EmailTokens (UserId, TokenHash, Purpose, ExpiresAt, Used)
            VALUES (@UserId, @TokenHash, @PurposeUserConfirm, @ExpiresAt, 0)", conn);
                userTokenCmd.Parameters.AddWithValue("@UserId", newUserId);
                userTokenCmd.Parameters.AddWithValue("@TokenHash", TokenHelper.HashToken(userToken));
                userTokenCmd.Parameters.AddWithValue("@PurposeUserConfirm", EmailTokenPurpose.UserConfirm);
                userTokenCmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddHours(24));
                await userTokenCmd.ExecuteNonQueryAsync();

                // Admin onay tokeni ekle
                SqlCommand adminTokenCmd = new SqlCommand(@"
            INSERT INTO EmailTokens (UserId, TokenHash, Purpose, ExpiresAt, Used)
            VALUES (@UserId, @TokenHash, @PurposeAdminApprove, @ExpiresAt, 0)", conn);
                adminTokenCmd.Parameters.AddWithValue("@UserId", newUserId);
                adminTokenCmd.Parameters.AddWithValue("@TokenHash", TokenHelper.HashToken(adminToken));
                adminTokenCmd.Parameters.AddWithValue("@PurposeAdminApprove", EmailTokenPurpose.AdminApprove);
                adminTokenCmd.Parameters.AddWithValue("@ExpiresAt", DateTime.UtcNow.AddHours(24));
                await adminTokenCmd.ExecuteNonQueryAsync();
            }

            // Mail gönderimi
            await _emailHelper.SendUserConfirmationAsync(model.Email, userToken);

            // Admin maili: Admin emaili veritabanından alınıyor
            string adminEmail = null;
            using (var conn = _baglanti.GetConnection())
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand("SELECT TOP 1 Email FROM Users WHERE Role='Admin'", conn);
                object result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    adminEmail = result.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                // Admin e-postası bulunamazsa uygun bir hata mesajı dön
                return StatusCode(500, new { message = "Admin e-posta adresi veritabanında bulunamadı. Lütfen en az bir admin kullanıcının tanımlandığından emin olun." });
            }
            await _emailHelper.SendAdminApprovalAsync(adminEmail, adminToken, model.Email);

            return Ok(new { message = "✅ Kayıt başarılı. Admin onayı bekleniyor." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest(new { message = "Eksik bilgi." });

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand(@"
            SELECT Id, PasswordHash, PasswordSalt, IsEmailConfirmed, IsActive, Role, FailedLoginCount, LockoutEnd
            FROM Users WHERE Email=@e", conn);
                cmd.Parameters.AddWithValue("@e", model.Email);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        return Unauthorized(new { message = "E-posta bulunamadı." });

                    int userId = reader.GetInt32(0);
                    string storedHash = reader.GetString(1);
                    string salt = reader.GetString(2); // PasswordSalt
                    bool confirmed = reader.GetBoolean(3); // IsEmailConfirmed
                    bool isActive = reader.GetBoolean(4);
                    string userRole = reader.GetString(5);
                    int failedLoginAttempts = reader.GetInt32(6); // FailedLoginCount
                    DateTime? lockoutEndTime = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7); // LockoutEnd

                    reader.Close(); // Reader'ı burada kapat, çünkü sonraki sorgular için aynı bağlantı kullanılacak.

                    // Hesabın kilitli olup olmadığını kontrol et
                    if (lockoutEndTime.HasValue && lockoutEndTime.Value > DateTime.UtcNow)
                    {
                        TimeSpan remainingLockout = lockoutEndTime.Value - DateTime.UtcNow;
                        return Unauthorized(new { message = $"❌ Hesabınız {Math.Ceiling(remainingLockout.TotalMinutes)} dakika boyunca kilitlendi." });
                    }

                    if (!confirmed)
                        return Unauthorized(new { message = "E-posta doğrulanmamış." });

                    if (!isActive)
                        return Unauthorized(new { message = "Kullanıcı admin onayı bekliyor." });

                    // Şifre doğrulama
                    if (!HashingHelper.VerifyPassword(model.Password, storedHash, salt))
                    {
                        // Başarısız giriş denemesini artır
                        failedLoginAttempts++;
                        SqlCommand updateAttemptsCmd = new SqlCommand(
                            "UPDATE Users SET FailedLoginCount=@attempts, LockoutEnd=@lockout WHERE Id=@userId", conn);
                        updateAttemptsCmd.Parameters.AddWithValue("@attempts", failedLoginAttempts);
                        updateAttemptsCmd.Parameters.AddWithValue("@userId", userId); // userId parametresi eklendi

                        DateTime? newLockoutEndTime = null;
                        string errorMessage;

                        if (failedLoginAttempts >= 5)
                        {
                            newLockoutEndTime = DateTime.UtcNow.AddMinutes(10);
                            errorMessage = "❌ 5 hatalı giriş denemesi yaptınız. Hesabınız 10 dakika boyunca kilitlendi.";
                        }
                        else
                        {
                            errorMessage = $"❌ Hatalı şifre. Kalan deneme hakkınız: {5 - failedLoginAttempts}.";
                        }
                        updateAttemptsCmd.Parameters.AddWithValue("@lockout", (object)newLockoutEndTime ?? DBNull.Value);
                        await updateAttemptsCmd.ExecuteNonQueryAsync();

                        return Unauthorized(new { message = errorMessage });
                    }
                    else
                    {
                        // Başarılı giriş, denemeleri sıfırla ve kilitlenmeyi kaldır
                        if (failedLoginAttempts > 0 || lockoutEndTime.HasValue)
                        {
                            SqlCommand resetAttemptsCmd = new SqlCommand(
                                "UPDATE Users SET FailedLoginCount=0, LockoutEnd=NULL WHERE Id=@userId", conn);
                            resetAttemptsCmd.Parameters.AddWithValue("@userId", userId);
                            await resetAttemptsCmd.ExecuteNonQueryAsync();
                        }
                    }

                    try
                    {
                        Console.WriteLine($"DEBUG: Login metodu - Kullanıcı rolü: {userRole}");
                        // JWT Token oluştur
                        string jwtToken = TokenHelper.GenerateJwtToken(userId, model.Email, userRole);

                        // Başarılı giriş: Token ve mesajı döndür
                        return Ok(new { token = jwtToken, message = "✅ Giriş başarılı!" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"DEBUG: InvalidOperationException in Login: {ex.Message}");
                        return StatusCode(500, new { message = "❌ Sunucu yapılandırma hatası oluştu." });
                    }
                    catch (SqlException ex) // SqlException için özel yakalama
                    {
                        // Veritabanı ile ilgili detaylı hata mesajı döndür
                        Console.WriteLine($"DEBUG: SQL Hatası (Login): {ex.Message} -- StackTrace: {ex.StackTrace}");
                        return StatusCode(500, new { message = "❌ Veritabanı hatası oluştu. Lütfen Users tablonuzdaki sütun adlarının ve tiplerinin doğru olduğundan emin olun. Hata Detayı: " + ex.Message });
                    }
                    catch (Exception ex) // Diğer genel hataları yakala
                    {
                        Console.WriteLine($"DEBUG: Beklenmeyen Hata (Login): {ex.Message} -- StackTrace: {ex.StackTrace}");
                        return StatusCode(500, new { message = "❌ Giriş sırasında beklenmeyen bir hata oluştu: " + ex.Message });
                    }
                }
            }
        }


        // 3️⃣ Admin onayı
        [HttpGet("approveuser")]
        [AllowAnonymous] // Link tıklayınca JWT gerekmez, token yeterli
        public async Task<IActionResult> ApproveUser([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token eksik." });

            string hashedToken = TokenHelper.HashToken(token);

            using (var conn = _baglanti.GetConnection())
            {
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 1 Id, UserId, ExpiresAt, Used
            FROM EmailTokens
            WHERE TokenHash=@hashed AND Purpose=@PurposeAdminApprove", conn);
                cmd.Parameters.AddWithValue("@hashed", hashedToken);
                cmd.Parameters.AddWithValue("@PurposeAdminApprove", EmailTokenPurpose.AdminApprove);

                int tokenId = 0;
                int userId = 0;
                DateTime expires = DateTime.MinValue;
                bool used = false;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        return BadRequest(new { message = "❌ Admin onay tokeni bulunamadı veya geçersiz." });

                    tokenId = reader.GetInt32(0);
                    userId = reader.GetInt32(1);
                    expires = reader.GetDateTime(2);
                    used = reader.GetBoolean(3);
                }

                if (used)
                    return BadRequest(new { message = "❌ Admin onay tokeni zaten kullanılmış." });
                if (expires < DateTime.UtcNow)
                    return BadRequest(new { message = "❌ Admin onay tokeninin süresi dolmuş." });

                // Kullanıcıyı aktif et
                SqlCommand activateUser = new SqlCommand(
                    "UPDATE Users SET IsActive=1 WHERE Id=@userId", conn);
                activateUser.Parameters.AddWithValue("@userId", userId);
                await activateUser.ExecuteNonQueryAsync();

                // Token’i kullanılmış yap
                SqlCommand updateToken = new SqlCommand(
                    "UPDATE EmailTokens SET Used=1 WHERE Id=@tokenId", conn);
                updateToken.Parameters.AddWithValue("@tokenId", tokenId);
                await updateToken.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "✅ Kullanıcı başarıyla aktif edildi. Artık giriş yapabilir." });
        }


        [HttpPost("changerole/{userId}")]
        [Authorize]
        public IActionResult ChangeRole(int userId, [FromQuery] string newRole)
        {
            // JWT'den rolü kontrol et
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Unauthorized(new { message = "❌ Bu işlemi sadece Admin kullanıcılar yapabilir." });

            // newRole kontrolü
            newRole = newRole?.Trim();
            if (newRole != "Admin" && newRole != "User")
                return BadRequest(new { message = "❌ Rol yalnızca 'Admin' veya 'User' olabilir." });

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();

                // Kullanıcı var mı?
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT Email, Role FROM Users WHERE Id=@id", conn);
                checkCmd.Parameters.AddWithValue("@id", userId);

                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound(new { message = "❌ Kullanıcı bulunamadı." });

                string targetEmail = reader.GetString(0);
                string currentRole = reader.GetString(1);
                reader.Close();

                // Kendini adminlikten düşürmeyi engelle
                string myEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (targetEmail == myEmail && newRole == "User")
                {
                    return BadRequest(new { message = "❌ Kendinizi adminlikten alamazsınız." });
                }

                // Güncelle
                SqlCommand updateCmd = new SqlCommand(
                    "UPDATE Users SET Role=@role WHERE Id=@id", conn);
                updateCmd.Parameters.AddWithValue("@role", newRole);
                updateCmd.Parameters.AddWithValue("@id", userId);
                updateCmd.ExecuteNonQuery();

                return Ok(new { message = $"✅ Kullanıcının rolü '{currentRole}' → '{newRole}' olarak değiştirildi." });
            }
        }

        // 7️⃣ Admin kullanıcı listeleme
        [HttpGet("listusers")]
        [Authorize]
        public IActionResult ListUsers()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Unauthorized(new { message = "❌ Sadece Admin kullanıcılar tüm kullanıcıları görebilir." });

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT Id, Email, Username, Role, IsActive, IsEmailConfirmed, CreatedAt
            FROM Users
            ORDER BY CreatedAt DESC", conn);

                using var reader = cmd.ExecuteReader();
                var users = new List<object>();

                while (reader.Read())
                {
                    users.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Email = reader.GetString(1),
                        Username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Role = reader.GetString(3),
                        IsActive = reader.GetBoolean(4),
                        IsEmailConfirmed = reader.GetBoolean(5),
                        CreatedAt = reader.GetDateTime(6)
                    });
                }

                return Ok(users);
            }
        }

        // 8️⃣ Admin kullanıcı silme
        [HttpDelete("deleteuser/{userId}")]
        [Authorize]
        public IActionResult DeleteUser(int userId)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (roleClaim != "Admin")
                return Unauthorized(new { message = "❌ Sadece Admin kullanıcılar silebilir." });

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();

                // Kullanıcı var mı?
                SqlCommand checkCmd = new SqlCommand("SELECT Email, Role FROM Users WHERE Id=@id", conn);
                checkCmd.Parameters.AddWithValue("@id", userId);
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound(new { message = "❌ Kullanıcı bulunamadı." });

                string targetEmail = reader.GetString(0);
                string targetRole = reader.GetString(1);
                reader.Close();

                // Kendini silme koruması
                string myEmail = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                if (targetEmail == myEmail)
                    return BadRequest(new { message = "❌ Kendinizi silemezsiniz. Önce başka admin atayın." });

                // Sil
                SqlCommand deleteCmd = new SqlCommand("DELETE FROM Users WHERE Id=@id", conn);
                deleteCmd.Parameters.AddWithValue("@id", userId);
                deleteCmd.ExecuteNonQuery();

                return Ok(new { message = $"🗑️ Kullanıcı '{targetEmail}' başarıyla silindi." });
            }
        }

        // 2️⃣ E-posta doğrulama (Admin onay)
        [HttpGet("verifyemail")]
        public IActionResult VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token eksik." });

            string hashedInput = TokenHelper.HashToken(token);

            try
            {
                using (SqlConnection conn = _baglanti.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 Id, UserId, ExpiresAt, Used
                FROM EmailTokens
                WHERE TokenHash=@hashed AND Purpose=@PurposeEmailConfirm", conn);
                    cmd.Parameters.AddWithValue("@hashed", hashedInput);
                    cmd.Parameters.AddWithValue("@PurposeEmailConfirm", EmailTokenPurpose.EmailConfirm);

                    int tokenId = 0;
                    int userId = 0;
                    DateTime expiresAt = DateTime.MinValue;
                    bool used = false;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return BadRequest(new { message = "❌ E-posta doğrulama tokeni bulunamadı veya geçersiz." });

                        tokenId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        userId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        expiresAt = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                        used = reader.IsDBNull(3) ? false : reader.GetBoolean(3);
                    }

                    if (used)
                        return BadRequest(new { message = "❌ E-posta doğrulama tokeni zaten kullanılmış." });
                    if (expiresAt < DateTime.UtcNow)
                        return BadRequest(new { message = "❌ E-posta doğrulama tokeninin süresi dolmuş." });

                    // Kullanıcıyı onayla
                    SqlCommand confirmCmd = new SqlCommand(
                        "UPDATE Users SET IsEmailConfirmed=1 WHERE Id=@userId", conn);
                    confirmCmd.Parameters.AddWithValue("@userId", userId);
                    confirmCmd.ExecuteNonQuery();

                    // Token’i kullanılmış olarak işaretle
                    SqlCommand updateCmd = new SqlCommand(
                        "UPDATE EmailTokens SET Used=1 WHERE Id=@tokenId", conn);
                    updateCmd.Parameters.AddWithValue("@tokenId", tokenId);
                    updateCmd.ExecuteNonQuery();

                    return Ok(new { message = "✅ E-posta başarıyla doğrulandı!" });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "❌ E-posta doğrulama sırasında beklenmeyen bir hata oluştu." }); // Daha genel mesaj
            }
        }

        // 2️⃣ Kullanıcı e-posta doğrulama
        [HttpGet("confirmemail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token eksik." });

            string hashedToken = TokenHelper.HashToken(token);

            using (var conn = _baglanti.GetConnection())
            {
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 1 Id, UserId, ExpiresAt, Used
            FROM EmailTokens
            WHERE TokenHash=@hashed AND Purpose=@PurposeUserConfirm", conn);
                cmd.Parameters.AddWithValue("@hashed", hashedToken);
                cmd.Parameters.AddWithValue("@PurposeUserConfirm", EmailTokenPurpose.UserConfirm);

                int tokenId = 0;
                int userId = 0;
                DateTime expires = DateTime.MinValue;
                bool used = false;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        return BadRequest(new { message = "❌ Kullanıcı doğrulama tokeni bulunamadı veya geçersiz." });

                    tokenId = reader.GetInt32(0);
                    userId = reader.GetInt32(1);
                    expires = reader.GetDateTime(2);
                    used = reader.GetBoolean(3);
                }

                if (used)
                    return BadRequest(new { message = "❌ Kullanıcı doğrulama tokeni zaten kullanılmış." });
                if (expires < DateTime.UtcNow)
                    return BadRequest(new { message = "❌ Kullanıcı doğrulama tokeninin süresi dolmuş." });

                // E-posta doğrulandı
                SqlCommand updateUser = new SqlCommand(
                    "UPDATE Users SET IsEmailConfirmed=1 WHERE Id=@userId", conn);
                updateUser.Parameters.AddWithValue("@userId", userId);
                await updateUser.ExecuteNonQueryAsync();

                // Token’i kullanılmış yap
                SqlCommand updateToken = new SqlCommand(
                    "UPDATE EmailTokens SET Used=1 WHERE Id=@tokenId", conn);
                updateToken.Parameters.AddWithValue("@tokenId", tokenId);
                await updateToken.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "✅ E-posta doğrulandı. Admin onayı bekleniyor." });
        }


        [HttpPost("forgotpassword")]
        [AllowAnonymous] // Herkese açık olmalı
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { message = "E-posta adresi gerekli." });

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();

                // Kullanıcı var mı kontrol
                SqlCommand cmd = new SqlCommand("SELECT Id FROM Users WHERE Email=@e", conn);
                cmd.Parameters.AddWithValue("@e", model.Email);
                var userIdObj = cmd.ExecuteScalar();
                if (userIdObj == null)
                    return BadRequest(new { message = "Bu e-posta kayıtlı değil." });

                int userId = Convert.ToInt32(userIdObj);

                // Token oluştur ve hashle
                string token = TokenHelper.GenerateToken();
                string tokenHash = TokenHelper.HashToken(token);

                SqlCommand insertToken = new SqlCommand(@"
            INSERT INTO PasswordResetTokens (UserId, TokenHash, ExpiresAt) 
            VALUES (@userId, @tokenHash, @expires)", conn);
                insertToken.Parameters.AddWithValue("@userId", userId);
                insertToken.Parameters.AddWithValue("@tokenHash", tokenHash);
                insertToken.Parameters.AddWithValue("@expires", DateTime.UtcNow.AddHours(1));
                insertToken.ExecuteNonQuery();

                // 🔗 Link simülasyonu
                string encodedToken = WebUtility.UrlEncode(token);
                string resetUrl = $"{_appConfig.BaseUrl}/resetpassword.html?token={encodedToken}"; // Dinamik URL, appsettings.json'dan okunur
                // Console.WriteLine($"[Simülasyon] Şifre sıfırlama linki: {resetUrl}"); // Konsola yazmayı kaldır

                // E-posta gönder
                string subject = "Şifre Sıfırlama Talebi";
                string body = $"Merhaba,<br><br>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın: <a href=\"{resetUrl}\">{resetUrl}</a><br><br>Bu link 1 saat boyunca geçerlidir.<br><br>Eğer bu isteği siz yapmadıysanız, bu e-postayı dikkate almayın.<br><br>Saygılarımızla,<br>SALASH Ekibi";
                await _emailHelper.SendEmailAsync(model.Email, subject, body);
            }

            return Ok(new { message = "✅ Şifre sıfırlama linki e-posta adresinize gönderildi." });
        }

        [HttpGet("/resetpassword.html")] // Mutlak URL olarak tanımla
        [AllowAnonymous] // Bu sayfa herkese açık olmalı
        public IActionResult ResetPasswordPage([FromQuery] string token)
        {
            // Token'ı direkt olarak HTML sayfasına göndermek yerine,
            // JavaScript üzerinden erişilebilir hale getirebiliriz.
            // Şimdilik, sadece sayfayı döndürelim.
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resetpassword.html"), "text/html");
        }

        [HttpPost("resetpassword")]
        [AllowAnonymous] // Şifre sıfırlama işlemi herkese açık
        public IActionResult ResetPassword([FromQuery] string token, [FromBody] ResetPasswordModel model)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token eksik." });

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Şifreler uyuşmuyor." });

            string tokenHash = TokenHelper.HashToken(token);

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT TOP 1 Id, UserId, ExpiresAt, Used
            FROM PasswordResetTokens
            WHERE TokenHash=@tokenHash", conn);
                cmd.Parameters.AddWithValue("@tokenHash", tokenHash);

                int tokenId = 0;
                int userId = 0;
                DateTime expires = DateTime.MinValue;
                bool used = false;

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return BadRequest(new { message = "❌ Şifre sıfırlama tokeni bulunamadı veya geçersiz." });

                    tokenId = reader.GetInt32(0);
                    userId = reader.GetInt32(1);
                    expires = reader.GetDateTime(2);
                    used = reader.GetBoolean(3);
                }

                if (used)
                    return BadRequest(new { message = "❌ Şifre sıfırlama tokeni zaten kullanılmış." });
                if (expires < DateTime.UtcNow)
                    return BadRequest(new { message = "❌ Şifre sıfırlama tokeninin süresi dolmuş." });

                // 🔐 Yeni salt oluştur
                string newSalt = HashingHelper.GenerateSalt();

                // 🔐 Şifreyi hashle
                string newHash = HashingHelper.HashPassword(model.Password, newSalt);

                // 🔄 Kullanıcıyı güncelle (hash + salt)
                SqlCommand updateUser = new SqlCommand(
                    "UPDATE Users SET PasswordHash=@p, PasswordSalt=@s WHERE Id=@userId", conn);
                updateUser.Parameters.AddWithValue("@p", newHash);
                updateUser.Parameters.AddWithValue("@s", newSalt);
                updateUser.Parameters.AddWithValue("@userId", userId);
                updateUser.ExecuteNonQuery();

                // Token’i kullanılmış yap
                SqlCommand updateToken = new SqlCommand(
                    "UPDATE PasswordResetTokens SET Used=1 WHERE Id=@tokenId", conn);
                updateToken.Parameters.AddWithValue("@tokenId", tokenId);
                updateToken.ExecuteNonQuery();
            }

            return Ok("✅ Şifre başarıyla sıfırlandı."); // Doğrudan string döndür
        }


    }
}
