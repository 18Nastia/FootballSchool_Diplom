using FootballSchool.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Настройка сервисов
builder.Services.AddRazorPages(options =>
{
    // Ограничиваем доступ ко всем страницам по умолчанию
    options.Conventions.AuthorizeFolder("/Main_Pages");
});

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// НАСТРОЙКА АУТЕНТИФИКАЦИИ
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Error";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddDbContext<FootballSchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

var uploadsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FootballSchool",
    "Uploads");

Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseStaticFiles();

app.UseRouting();

// Включаем аутентификацию и авторизацию
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Редирект в зависимости от авторизации
app.MapGet("/", async (HttpContext context) =>
{
    if (!context.User.Identity.IsAuthenticated)
    {
        return Results.Redirect("/Login");
    }

    if (context.User.IsInRole("Admin"))
    {
        return Results.Redirect("/Main_Pages/Index_Admin");
    }
    else
    {
        return Results.Redirect("/Main_Pages/Index_Parent");
    }
});

app.Run();