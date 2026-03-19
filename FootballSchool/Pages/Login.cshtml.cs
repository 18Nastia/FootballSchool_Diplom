using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FootballSchool.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Pages
{
    public class LoginModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public LoginModel(FootballSchoolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Login { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
        public string InfoMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Автоматическое создание админа при первом запуске
            if (!await _context.Users.AnyAsync())
            {
                var admin = new User
                {
                    Login = "admin",
                    Password = "admin",
                    Role = "Admin"
                };
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();
                InfoMessage = "Система инициализирована. Создан аккаунт администратора: Логин - admin, Пароль - admin. Рекомендуется сменить пароль после входа.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Пожалуйста, введите логин и пароль.";
                return Page();
            }

            // Ищем пользователя в БД
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == Login && u.Password == Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("UserId", user.UserId.ToString())
                };

                if (user.Role == "Coach")
                {
                    // Извлекаем ID тренера из логина (формат: coach_имя_айди)
                    var parts = user.Login.Split('_');
                    if (parts.Length > 0)
                    {
                        var coachId = parts.Last(); // Берем последнюю часть после подчеркивания
                        claims.Add(new Claim("CoachId", coachId));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (user.Role == "Admin")
                    return RedirectToPage("/Main_Pages/Index_Admin");
                else if (user.Role == "Coach")
                    return RedirectToPage("/Main_Pages/Index_Coach");
                else
                    return RedirectToPage("/Main_Pages/Index_Parent");
            }

            ErrorMessage = "Неверный логин или пароль.";
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }
    }
}