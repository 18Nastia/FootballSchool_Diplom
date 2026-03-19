using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using FootballSchool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace FootballSchool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly FootballSchoolContext _context;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IConfiguration _config;

        public AccountController(FootballSchoolContext context, IDataProtectionProvider dataProtectionProvider, IConfiguration config)
        {
            _context = context;
            _dataProtectionProvider = dataProtectionProvider;
            _config = config;
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!User.Identity!.IsAuthenticated) return Unauthorized("Вы не авторизованы.");

            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return BadRequest("Пользователь не найден.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Пользователь не найден.");

            if (user.Password != request.OldPassword)
                return BadRequest("Неверный текущий пароль.");

            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пароль успешно изменен." });
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email)) return BadRequest("Email обязателен");

            try
            {
                // Ищем пользователя по Email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    return Ok(new { message = "Если email существует в системе, письмо отправлено." });
                }

                // 1. Создаем защищенный Stateless Токен
                var protector = _dataProtectionProvider.CreateProtector("PasswordReset");
                long expirationTicks = DateTime.UtcNow.AddHours(1).Ticks; // Токен живет 1 час

                // Формат строки: логин|время_истечения
                string unencryptedToken = $"{user.Login}|{expirationTicks}";
                string encryptedToken = protector.Protect(unencryptedToken);
                string urlSafeToken = WebUtility.UrlEncode(encryptedToken); // Делаем безопасным для URL

                // 2. Формируем ссылку
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                string resetLink = $"{baseUrl}/ResetPassword?token={urlSafeToken}";

                // 3. Достаем настройки SMTP
                string smtpHost = _config["SmtpSettings:Host"];
                int smtpPort = int.Parse(_config["SmtpSettings:Port"]);
                string smtpEmail = _config["SmtpSettings:Email"];
                string smtpPassword = _config["SmtpSettings:Password"];

                // 4. Отправляем письмо
                using (var smtpClient = new SmtpClient(smtpHost))
                {
                    smtpClient.Port = smtpPort;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpEmail, "FootballSchool"),
                        Subject = "Сброс пароля - FootballSchool",
                        Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; max-width: 600px; border: 1px solid #ddd; border-radius: 8px;'>
                                <h3 style='color: #2563EB;'>Восстановление пароля</h3>
                                <p>Вы запросили сброс пароля для аккаунта FootballSchool.</p>
                                <p>Для создания нового пароля перейдите по ссылке ниже:</p>
                                <p><a href='{resetLink}' style='display: inline-block; padding: 10px 20px; background-color: #2563EB; color: white; text-decoration: none; border-radius: 5px;'>Сбросить пароль</a></p>
                                <p>Или скопируйте эту ссылку в браузер:</p>
                                <p style='word-break: break-all; color: #555;'><small>{resetLink}</small></p>
                                <br/>
                                <p style='color: #888;'><i>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо. Ссылка действительна 1 час.</i></p>
                            </div>",
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(request.Email);

                    await smtpClient.SendMailAsync(mailMessage);
                }

                return Ok(new { message = "Письмо с инструкциями успешно отправлено на вашу почту." });
            }
            catch (SmtpException smtpEx)
            {
                // Специфичная ошибка SMTP (например, 535 Authentication Failed)
                return StatusCode(500, $"Ошибка SMTP Mail.ru: {smtpEx.Message} (Код: {smtpEx.StatusCode})");
            }
            catch (Exception ex)
            {
                // Общая ошибка
                return StatusCode(500, $"Общая ошибка отправки: {ex.Message}");
            }
        }

    }
}