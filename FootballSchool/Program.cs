using FootballSchool.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку Razor Pages
builder.Services.AddRazorPages();

// РЕГИСТРАЦИЯ БАЗЫ ДАННЫХ (Это главное для функциональности!)
// Сообщаем приложению, что нужно использовать SQL Server
builder.Services.AddDbContext<FootballSchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Настройка конвейера HTTP-запросов.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Настраиваем маршрутизацию
app.MapRazorPages();

// Перенаправляем корневой URL на вашу страницу
app.MapGet("/", () => Results.Redirect("/Main_Pages/Index_Admin"));

// Или альтернативный вариант - использовать MapFallback
// app.MapFallbackToPage("/Main_Pages/Index_Admin");

app.Run();