using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
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