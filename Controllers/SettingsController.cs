using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MuhasebeAPI.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json; // Newtonsoft.Json eklendi
using Newtonsoft.Json.Linq; // JObject için eklendi

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
            _appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
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
                var jsonString = await System.IO.File.ReadAllTextAsync(_appSettingsPath);
                var json = JObject.Parse(jsonString);

                // EmailSettings bölümünü güncelle
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
