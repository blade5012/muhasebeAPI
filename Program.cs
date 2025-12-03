using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using MuhasebeAPI.Helpers; // Baglanti sınıfı için namespace
using MuhasebeAPI.Models; // EmailSettings modeli için namespace
using Microsoft.Extensions.Options; // IOptions için

var builder = WebApplication.CreateBuilder(args);

// Servis olarak çalıştığını Windows'a bildir
builder.Host.UseWindowsService();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// E-posta ayarlarını yükle
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailHelper>(sp =>
{
    var emailSettingsMonitor = sp.GetRequiredService<IOptionsMonitor<EmailSettings>>(); // Monitor kullanıldı
    var baglanti = sp.GetRequiredService<Baglanti>();
    var appConfig = sp.GetRequiredService<IOptions<AppConfiguration>>();
    return new EmailHelper(emailSettingsMonitor, baglanti, appConfig);
}); // EmailHelper'ı bağımlılıklarıyla Scoped olarak kaydet
builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection("AppConfiguration")); // AppConfiguration yükle

// Baglanti sınıfı için DI
builder.Services.AddScoped<Baglanti>();

// JWT Ayarları
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
        };
    });
builder.Services.AddAuthorization();

// CORS (Her yerden erişim)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// !!! URL burada ayarlanıyor !!!
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// TokenHelper'ı IConfiguration ile başlat
TokenHelper.Initialize(builder.Configuration);

var app = builder.Build();

// Swagger sadece Development'ta aktif olsun
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Statik dosyalar
app.UseDefaultFiles();   // index.html otomatik açılsın
app.UseStaticFiles();    // wwwroot aktif

app.UseRouting(); // Routing'i başlat

// CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Controller Routes
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();

