using Grupp3_Login_API.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lägg till databaskoppling
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Lägg till autentisering med cookie-baserad autentisering
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "YourAppNameCookie"; // Anpassa cookie-namnet
        options.LoginPath = "/api/authentication/login"; // API-vänlig login-URL
        options.LogoutPath = "/api/authentication/logout"; // API-vänlig logout-URL
        options.SlidingExpiration = true; // Förlänger sessionen vid aktivitet
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10); // Timeout efter 30 minuter
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Endast över HTTPS
        options.Cookie.HttpOnly = true; // Förhindrar åtkomst via JavaScript
        options.Cookie.SameSite = SameSiteMode.Strict; // Förhindrar CSRF
    });

// Lägg till CORS-policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:7291")  // Lägg till din MVC-applikations URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Tillåt cookies att skickas
    });
});

// Lägg till Controllers
builder.Services.AddControllers(); // Denna måste finnas för att använda API:et

// Lägg till Swagger för utveckling
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.UseCors("AllowSpecificOrigins"); // Använd CORS-policy
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
