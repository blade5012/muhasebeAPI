
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http; // IHttpContextAccessor için eklendi

using MuhasebeAPI.Helpers; // Baglanti sınıfı için namespace
using MuhasebeAPI.Models; // EmailSettings modeli için namespace
using Microsoft.Extensions.Options; // IOptions için

var builder = WebApplication.CreateBuilder(args);

// Servis olarak çalıştığını Windows'a bildir


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
    var appConfig = sp.GetRequiredService<AppConfiguration>(); // Doğrudan AppConfiguration nesnesini al
    return new EmailHelper(emailSettingsMonitor, baglanti, Options.Create(appConfig)); // Options.Create ile sarmala
}); // EmailHelper'ı bağımlılıklarıyla Scoped olarak kaydet

// IHttpContextAccessor'ı ekle
builder.Services.AddHttpContextAccessor();

// AppConfiguration'ı dinamik olarak ayarla
builder.Services.AddScoped(sp => {
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var request = httpContextAccessor.HttpContext?.Request;
    
    string baseUrl = "";
    if (request != null) {
        string scheme = request.Scheme ?? "http"; // Null ise varsayılan olarak http
        string host = request.Host.Value ?? "localhost:5000"; // Null ise varsayılan host
        baseUrl = $"{scheme}://{host}";
    } else {
        // HttpContext.Request null ise, varsayılan bir URL kullan
        // Genellikle uygulamanın dinlediği adres kullanılır. Program.cs'deki UseUrls ile eşleşmeli.
        baseUrl = "http://0.0.0.0:5000"; // veya uygulamanın çalıştığı varsayılan adres
    }

    return new AppConfiguration { BaseUrl = baseUrl };
});

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

// Railway port ayarı - PORT environment variable'ını kullan
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

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



// Program.cs'nin SONUNA, app.Run()'dan önce ekleyin:
app.MapGet("/api/test/sql", async (Baglanti baglanti) =>
{
    try
    {
        using var conn = baglanti.GetConnection();
        await conn.OpenAsync();
        return Results.Ok(new
        {
            success = true,
            message = "✅ SQL Server bağlantısı BAŞARILI",
            server = conn.DataSource,
            database = conn.Database,
            source = "Railway MSSQL_URL"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "❌ SQL Server bağlantısı BAŞARISIZ",
            detail: $"Hata: {ex.Message}\nMSSQL_URL: {Environment.GetEnvironmentVariable("MSSQL_URL")}",
            statusCode: 500
        );
    }
});




app.Run();

