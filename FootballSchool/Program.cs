using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ��������� ������� Razor Pages
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

// ����������� �������������
app.MapRazorPages();

// �������������� �������� URL �� ���� ��������
app.MapGet("/", () => Results.Redirect("/Main_Pages/Index_Admin"));

// ��� �������������� ������� - ������������ MapFallback
// app.MapFallbackToPage("/Main_Pages/Index_Admin");

app.Run();