using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MuhasebeAPI.Models;
using System.Data.SqlClient;

namespace MuhasebeAPI.Helpers
{
    public class EmailHelper
    {
        private readonly IOptionsMonitor<EmailSettings> _emailSettingsMonitor; // IOptionsMonitor olarak değiştirildi
        private readonly Baglanti _baglanti;
        private readonly IOptions<AppConfiguration> _appConfig; // AppConfiguration IOptions olarak bırakıldı

        public EmailHelper(IOptionsMonitor<EmailSettings> emailSettingsMonitor, Baglanti baglanti, IOptions<AppConfiguration> appConfig)
        {
            _emailSettingsMonitor = emailSettingsMonitor; // Monitor atanıyor
            _baglanti = baglanti;
            _appConfig = appConfig; // IOptions olarak atanıyor
        }

        // Genel mail gnderimi
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var currentEmailSettings = _emailSettingsMonitor.CurrentValue; // En güncel ayarlar alınıyor
            using (var client = new SmtpClient(currentEmailSettings.SmtpServer, currentEmailSettings.SmtpPort))
            {
                client.Credentials = new NetworkCredential(currentEmailSettings.SmtpUsername, currentEmailSettings.SmtpPassword);
                client.EnableSsl = true;

                var mail = new MailMessage(currentEmailSettings.SenderEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                await client.SendMailAsync(mail);
            }
        }

        public async Task SendUserConfirmationAsync(string toEmail, string token)
        {
            string confirmLink = $"{_appConfig.Value.BaseUrl}/api/auth/confirmemail?token={WebUtility.UrlEncode(token)}"; // BaseUrl kullanıldı
            string subject = "Lütfen e-postanızı doğrulayın";
            string body = $@"
        <h3>Merhaba,</h3>
        <p>Kayıt işleminizi tamamlamak için aşağıdaki linke tıklayın:</p>
        <a href='{confirmLink}'>E-postayı doğrula</a>
        <p>Link 24 saat geçerlidir.</p>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAdminApprovalAsync(string adminEmail, string token, string newUserEmail)
        {
            string approveLink = $"{_appConfig.Value.BaseUrl}/api/auth/approveuser?token={WebUtility.UrlEncode(token)}"; // BaseUrl kullanıldı
            string subject = "Yeni kullanıcı onayı gerekiyor";
            string body = $@"
        <h3>Merhaba Admin,</h3>
        <p>{newUserEmail} adlı kullanıcı kayıt oldu. Onaylamak için aşağıdaki linke tıklayın:</p>
        <a href='{approveLink}'>Kullanıcıyı Onayla</a>
        <p>Link 24 saat geçerlidir.</p>
    ";

            await SendEmailAsync(adminEmail, subject, body);
        }

       
    }
}
