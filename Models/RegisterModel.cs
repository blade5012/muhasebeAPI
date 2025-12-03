using System.ComponentModel.DataAnnotations;

namespace MuhasebeAPI.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Email alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; }
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter uzunluğunda olmalıdır.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z]).{6,}$", ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf ve en az 6 karakter içermelidir.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı alanı zorunludur.")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; }
    }
}
