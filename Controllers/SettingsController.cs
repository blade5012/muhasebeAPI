using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MuhasebeAPI.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MuhasebeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _appSettingsPath;

        public SettingsController(IConfiguration configuration)
        {
            _configuration = configuration;

            // 🔥 ÇÖZÜM: EXE'nin gerçek klasörünü al
            _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        }

        [HttpGet("email")]
        public IActionResult GetEmailSettings()
        {
            var emailSettings = new EmailSettings();
            _configuration.GetSection("EmailSettings").Bind(emailSettings);
            return Ok(emailSettings);
        }

        [HttpPost("email")]
        public async Task<IActionResult> UpdateEmailSettings([FromBody] EmailSettings updatedSettings)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (!System.IO.File.Exists(_appSettingsPath))
                {
                    return StatusCode(500, new { message = $"appsettings.json bulunamadı: {_appSettingsPath}" });
                }

                var jsonString = await System.IO.File.ReadAllTextAsync(_appSettingsPath);
                var json = JObject.Parse(jsonString);

                json["EmailSettings"]["SmtpServer"] = updatedSettings.SmtpServer;
                json["EmailSettings"]["SmtpPort"] = updatedSettings.SmtpPort;
                json["EmailSettings"]["SmtpUsername"] = updatedSettings.SmtpUsername;
                json["EmailSettings"]["SmtpPassword"] = updatedSettings.SmtpPassword;
                json["EmailSettings"]["SenderEmail"] = updatedSettings.SenderEmail;
                json["EmailSettings"]["SenderName"] = updatedSettings.SenderName;

                var updatedJsonString = JsonConvert.SerializeObject(json, Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(_appSettingsPath, updatedJsonString);

                return Ok(new { message = "Email ayarları başarıyla güncellendi." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Email ayarları güncellenirken bir hata oluştu: " + ex.Message });
            }
        }
    }
}
