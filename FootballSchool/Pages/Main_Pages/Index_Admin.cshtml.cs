using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FootballSchool.Pages.Main_Pages
{
    public class Index_AdminModel : PageModel
    {
        private readonly FootballSchoolContext _context;
        private readonly ILogger<Index_AdminModel> _logger;

        public Index_AdminModel(FootballSchoolContext context, ILogger<Index_AdminModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int TodayTrainingsCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public double WeeklyAttendancePercentage { get; set; }
        public int ActiveSubscriptionsCount { get; set; }
        public int PendingPaymentsCount { get; set; }
        public int CoachesCount { get; set; }

        public List<string> Notifications { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekAgo = today.AddDays(-7);

            TodayTrainingsCount = await _context.Training
                .CountAsync(t => t.DateTraining == today);

            TotalStudentsCount = await _context.Students.CountAsync();

            var recentAttendances = await _context.Attendances
                .Include(a => a.Training)
                .Where(a => a.Training.DateTraining >= weekAgo && a.Training.DateTraining <= today)
                .ToListAsync();

            if (recentAttendances.Any())
            {
                int presentCount = recentAttendances.Count(a => a.StatusAttendance == "Был");
                WeeklyAttendancePercentage = Math.Round((double)presentCount / recentAttendances.Count * 100);
            }
            else
            {
                WeeklyAttendancePercentage = 0;
            }

            ActiveSubscriptionsCount = await _context.Subscriptions.CountAsync();

            PendingPaymentsCount = await _context.Payments
                .CountAsync(p => p.StatusPayment == "В обработке" || p.StatusPayment == "Не оплачен");
            CoachesCount = await _context.Coaches.CountAsync();

            var upcomingTrainings = await _context.Training
                .Include(t => t.Team)
                .Where(t => t.DateTraining >= today)
                .OrderBy(t => t.DateTraining).ThenBy(t => t.TimeTraining)
                .Take(3)
                .ToListAsync();

            foreach (var t in upcomingTrainings)
            {
                string teamName = t.Team?.CategoryTeam ?? "Без группы";
                string dayStr = t.DateTraining == today ? "Сегодня" : t.DateTraining.ToString("dd.MM.yyyy");
                Notifications.Add($"Ближайшая тренировка группы «{teamName}»: {dayStr} в {t.TimeTraining.ToString("HH:mm")}");
            }

            if (!Notifications.Any())
            {
                Notifications.Add("На ближайшее время нет запланированных событий.");
            }
        }

        // Обработчик скрытой формы добавления администратора
        public async Task<IActionResult> OnPostAddAdminAsync(string adminLogin, string adminPassword, string? adminEmail)
        {
            if (string.IsNullOrWhiteSpace(adminLogin) || string.IsNullOrWhiteSpace(adminPassword))
            {
                TempData["ErrorMessage"] = "Логин и пароль обязательны для заполнения.";
                return RedirectToPage();
            }

            // Проверяем, существует ли уже пользователь с таким логином
            bool userExists = await _context.Users.AnyAsync(u => u.Login == adminLogin);
            if (userExists)
            {
                TempData["ErrorMessage"] = "Пользователь с таким логином уже существует.";
                return RedirectToPage();
            }

            try
            {
                var newAdmin = new User
                {
                    Login = adminLogin,
                    Password = adminPassword,
                    Email = adminEmail,
                    Role = "Admin"
                };

                _context.Users.Add(newAdmin);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Новый администратор «{adminLogin}» успешно зарегистрирован.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении администратора.");
                TempData["ErrorMessage"] = $"Произошла ошибка при регистрации: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}