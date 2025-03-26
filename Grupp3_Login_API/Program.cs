using Grupp3_Login_API.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Grupp3_Login_API.Data;
using Microsoft.Extensions.ObjectPool;

var builder = WebApplication.CreateBuilder(args);

// Lägg till databaskoppling
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));



// Lägg till autentisering med cookie-baserad autentisering
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "API_Cookie";
        options.SlidingExpiration = true; // Förlänger sessionen vid aktivitet
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Timeout efter 30 minuter
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Endast över HTTPS
        options.Cookie.HttpOnly = true; // Förhindrar åtkomst via JavaScript
        options.Cookie.SameSite = SameSiteMode.None;// Kanske ändra till strict senare
    });

// Lägg till Controllers
builder.Services.AddControllers(); // Denna måste finnas för att använda API:et

// Lägg till Swagger för utveckling
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Ska i produktion ändras med restriktioner
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowAll",
    builder => builder
        .WithOrigins("https://localhost:7291", "https://localhost:53694")
        .AllowAnyMethod()
        .AllowAnyHeader());
});


var app = builder.Build();

// Aktivera Swagger för utveckling
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grupp3 Login API v1");
        c.RoutePrefix = string.Empty; // Swagger öppnas på root-URL
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication(); // Aktivera autentisering
app.UseAuthorization(); // Aktivera auktorisering
app.MapControllers(); // API-kontroller

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Fel vid app.Run(): {ex.Message}");
    throw;
}
