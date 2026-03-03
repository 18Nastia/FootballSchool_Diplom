using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;
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

        // Свойства для отображения статистики
        public int TodayTrainingsCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public double WeeklyAttendancePercentage { get; set; }
        public int ActiveSubscriptionsCount { get; set; }
        public int PendingPaymentsCount { get; set; }
        public int CoachesCount { get; set; }

        // Динамический список уведомлений
        public List<string> Notifications { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekAgo = today.AddDays(-7);

            // 1. Считаем тренировки на сегодня
            TodayTrainingsCount = await _context.Training
                .CountAsync(t => t.DateTraining == today);

            // 2. Считаем общее количество учеников
            TotalStudentsCount = await _context.Students.CountAsync();

            // 3. Высчитываем % посещаемости за последние 7 дней
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

            // 4. Считаем абонементы (в данном случае просто все выданные, можно добавить логику проверки дат)
            ActiveSubscriptionsCount = await _context.Subscriptions.CountAsync();

            // 5. Считаем проблемные/ожидающие платежи
            PendingPaymentsCount = await _context.Payments
                .CountAsync(p => p.StatusPayment == "В обработке" || p.StatusPayment == "Не оплачен");

            // 6. Считаем тренеров
            CoachesCount = await _context.Coaches.CountAsync();

            // 7. Генерируем динамические уведомления (например, ближайшие 3 тренировки)
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

            // Проверка на случай отсутствия событий
            if (!Notifications.Any())
            {
                Notifications.Add("На ближайшее время нет запланированных событий.");
            }
        }
    }
}