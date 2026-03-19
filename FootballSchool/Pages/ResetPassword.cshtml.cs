using System;
using System.Threading.Tasks;
using FootballSchool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace FootballSchool.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly FootballSchoolContext _context;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public ResetPasswordModel(FootballSchoolContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _dataProtectionProvider = dataProtectionProvider;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Token))
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                ErrorMessage = "Токен сброса отсутствует.";
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return Page();
            }

            try
            {
                // Расшифровываем токен
                var protector = _dataProtectionProvider.CreateProtector("PasswordReset");
                string decryptedToken = protector.Unprotect(Token);

                // Разбиваем строку на логин и время истечения
                var parts = decryptedToken.Split('|');
                if (parts.Length != 2)
                {
                    ErrorMessage = "Недействительный токен.";
                    return Page();
                }

                string login = parts[0];
                long expirationTicks = long.Parse(parts[1]);

                // Проверяем срок действия
                if (DateTime.UtcNow.Ticks > expirationTicks)
                {
                    ErrorMessage = "Срок действия ссылки истек. Запросите сброс пароля снова.";
                    return Page();
                }

                // Ищем пользователя
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
                if (user == null)
                {
                    ErrorMessage = "Пользователь не найден.";
                    return Page();
                }

                // Меняем пароль
                user.Password = NewPassword;
                await _context.SaveChangesAsync();

                IsSuccess = true;
                return Page();
            }
            catch
            {
                ErrorMessage = "Недействительный или поврежденный токен.";
                return Page();
            }
        }
    }
}