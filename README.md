# Account Management API
Det här API:et kan ni använda för autentisering och kontohantering. Detta API används för att hantera inloggning, registering och hantering av konton. Samt en rollbaserad åtkomstkontroll i ett MVC-projekt.

## 📑Innehållsförteckning
1. [Introduktion](#introduktion)
2. [Konfiguration i MVC-projekt](#konfiguration-i-mvc-projekt)
3. [Autentisering & Auktorisering](#autentisering--auktorisering)
4. [API-endpoints](#api-endpoints)
5. [Exempel på API-anrop](#exempel-på-api-anrop)
6. [Kontakt](#kontakt)

---

## Introduktion

👋 Detta API är utvecklat för att hantera inloggning och kontohantering via en ASP.NET Core MVC-klient. API:et hanterar **cookie-baserad autentisering**, där en **cookie** returneras efter lyckad inloggning och används för efterföljande API-anrop.

API:et kommer att **hostas på högskolans server**, och du kan konsumera det utan att klona källkoden.

### **https://localhost:7200**

---

## Konfiguration i MVC-projekt

⚙️ Innan du anropar API:et behöver du säkerställa att ditt MVC-projekt är konfigurerat korrekt för autentisering och att hantera cookies.

### 1️⃣ **Lägg till nödvändiga paket**
📦 Se till att du har dessa paket installerade i ditt MVC-projekt:
```sh
dotnet add package Microsoft.AspNetCore.Authentication.Cookies
dotnet add package Microsoft.Extensions.Http
```

### 2️⃣ Konfigurera Program.cs
🔧 I Program.cs, konfigurera autentisering, session och HttpClient:

Du kanske redan har de flesta delarna i den här **program.cs**. Det här är exakt hur **program.cs** ser ut i vårt MVC-projekt.

Läs igenom noga och se vilka delar du själv saknar.

Tänk på att **using Grupp3_Login.Services;** ska istället vara modellerna från ditt egna projekt. Detsamma gäller när det kommer till **namespace** etc.

```csharp
using Grupp3_Login.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Lägg till session och cookie-baserad autentisering
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Grupp3_Login.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout för sessionen
    options.Cookie.IsEssential = true; // Gör sessionen nödvändig
});

// Lägg till HttpClient via IHttpClientFactory
builder.Services.AddHttpClient();

// Lägg till ApiService för att anropa API:et
builder.Services.AddScoped<ApiService>();

// Lägg till autentisering med cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login"; // Sidan du kommer till om du försöker navigera till en autentiserad sida utan att vara inloggad. Ändra till vad ni vill.
        options.LogoutPath = "/Home/Logout"; // Sidan du kommer till när du loggar ut och cookien tas bort. Ändra efter behov.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### 3️⃣ Skapa LoginRequest & LoginResponse
🚧 Lägg till en ny fil LoginModels.cs i mappen **Models** i ditt MVC-projekt.

Dessa modeller används när MVC-projektet skickar inloggningsuppgifter till API:et.



```csharp
namespace Grupp3_Login.Models
{
 
    //Modell för att skicka inloggningsuppgifter till API:et.
    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    // Modell för att ta emot API-svaret vid inloggning.
    public class LoginResponse
    {
        public string Role { get; set; }
    }
}
```



### 4️⃣ Lägg till en API-tjänst (ApiService.cs)
🐕‍🦺 Denna tjänst ansvarar för att anropa API:et och hantera HTTP-förfrågningar.

Använd här **modellen** för att skicka inloggningsuppgifter.

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Grupp3_Login.Models;

namespace Grupp3_Login.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> LoginAsync(LoginRequest model) // Modellen vi precis skapade skickar inloggningsuppgifter
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("https://api.dittdomän.com/api/Authentication/login", model);
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ett fel inträffade vid API-anropet: {ex.Message}", ex);
            }
        }
    }
}
```
### 5️⃣ Implementera inloggning i HomeController.cs

Här är HomeController från vårt MVC-projekt, men din behöver inte se exakt ut såhär. Plocka ut de delarna som du behöver för att konsumera API:et.

```csharp
﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Grupp3_Login.Models;
using Grupp3_Login.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class HomeController : Controller
{
    private readonly ApiService _apiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApiService apiService, ILogger<HomeController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Admin()
    {
        // Kontrollera om användaren är inloggad via cookies
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login"); // Om ej inloggad, omdirigera till login
        }

        return View();
    }

    // Hantera inloggning via API:et och skapa cookie
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model); // Om formuläret är felaktigt, visa det igen
        }

        // Skicka loginförfrågan till API:et
        var response = await _apiService.LoginAsync(model);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.Error = "Felaktigt användarnamn eller lösenord.";
            return View("Index");
        }

        // Läs API-svaret som en `LoginResponse`
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result == null || string.IsNullOrEmpty(result.Role))
        {
            _logger.LogError("API-svaret kunde inte deserialiseras korrekt!");
            ViewBag.Error = "Något gick fel. Försök igen";
            return View("Index"); // Visa login igen om något gick fel
        }

        // Skapa cookie med användartoken
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.UserName),
            new Claim(ClaimTypes.Role, result.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Logga in användaren med cookie
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        _logger.LogInformation($"Användaren {model.UserName} loggade in.");

        // Omdirigera beroende på roll
        return result.Role == "Admin" ? RedirectToAction("Admin") : RedirectToAction("Index");
    }

    // Logga ut
    public async Task<IActionResult> Logout()
    {
        // Logga ut användaren och rensa cookies
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login"); // Omdirigera till login
    }
}
```
---

## Autentisering & Auktorisering

- **Autentisering:** Användare loggar in via `POST /api/Authentication/login` och får en **cookie** vid lyckad inloggning. Cookien innehåller en token för auktorisering.
- Cookie varar i 30 minuter, vidare anropp förnyar sessionen, men det kan behövas ytterliggare kod om den ska förlängas utan ytterliggare anropp till API:et.
- Anroppas `POST /api/Authentication/logout` kommer cookien finns kvar tiden ut, men tokenen är inte längre tiltig.
  
- **Auktorisering:** Cookien används i efterföljande API-anrop för att identifiera användaren och dess roll. Flera av metoderna kräver att cookien med en **admin-token** skickas med för att de ska kunna konsumeras.

### **API:et stödjer följande roller:**
- **Admin** - Fullständig åtkomst till kontohantering.
- **Employee** - Kan hantera viss data.
- **User** - Kan endast hantera sitt eget konto.

---

## API-endpoints
### 🔌🔑 **Autentisering**
| Metod | Endpoint | Beskrivning |
|-------|---------|-------------|
| `POST` | `/api/Authentication/login` | Logga in och få en cookie |
| `POST` | `/api/Authentication/logout` | Logga ut och rensa cookie |

## 👥🔐 **Kontohantering**
| Metod | Endpoint | Beskrivning |
|-------|---------|-------------|
| `GET` | `/api/Account` | Hämta alla konton (Endast Admin) |
| `POST` | `/api/Account/register` | Skapa ett nytt användarkonto (User) |
| `POST` | `/api/Account/create-employee` | Skapa ett nytt Employee-konto (Endast Admin) |
| `PUT` | `/api/Account/{id}` | Uppdatera ett konto (Endast Admin) |
| `DELETE` | `/api/Account/{id}` | Ta bort ett konto (Endast Admin) |

---

## Exempel på API-anrop
### 🔑 **Logga in som Admin**
```http
POST /api/Auth/login
```
📥 Request Body (JSON)
➡️ Skicka inloggningsuppgifter till API:et
```json
{
    "userName": "adminUser",
    "password": "Admin123!"
}
```
📥 Svar från API:et (JSON)
➡️ Om inloggningen lyckas får du en cookie tillbaka
```json
{
    "role": "Admin"
}
```
✅ Notera:

API:et svarar med en cookie som lagras automatiskt av webbläsaren.
Cookien används för autentisering och skickas automatiskt med framtida anrop.

## 🍪 Hur du använder cookien i MVC

### 1️⃣ **Begränsa en View så att endast Admin kan se den**
I Views/Home/Admin.cshtml, kan du skydda innehållet så att endast Admins kan se det:
```csharp
@using System.Security.Claims

@if (User.IsInRole("Admin"))
{
    <h1>Välkommen, Admin!</h1>
    <p>Du har behörighet att hantera systemet.</p>
}
else
{
    <h1>Åtkomst nekad</h1>
    <p>Du har inte behörighet att se denna sida.</p>
}
```
➡️ Hur fungerar detta?

User.IsInRole("Admin") kontrollerar om den inloggade användaren har rollen Admin.

Om användaren inte är admin visas meddelandet "Åtkomst nekad" istället.

### 2️⃣ **Begränsa en Controller-metod till Admins**
I HomeController.cs, kan vi använda [Authorize] för att begränsa åtkomst:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    [Authorize(Roles = "Admin")] // Endast Admins kan anropa denna metod
    public IActionResult AdminPanel()
    {
        return View();
    }
}
```
➡️ Hur fungerar detta?

[Authorize(Roles = "Admin")] gör att endast användare med Admin-roll kan anropa metoden.

Om en användare utan Admin-roll försöker besöka /Home/AdminPanel, kommer de att få en 403 Forbidden-sida.

## **Kontakt**

💻 Ni kan enklast kontakta mig **Adam Karlsson** på **Discord**.
Lägg till mig @ **Kirriko#1242**

🎨 Eller på **Canvas** 

🏫 Annars ses vi på skolan på integrationsmöten eller SOA-workshops
