using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;
using Microsoft.AspNetCore.Authorization;

namespace FootballSchool.Pages.Main_Pages
{
    [Authorize(Roles = "Coach")]
    public class Index_CoachModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        // Внедрение зависимости для доступа к БД (решает ошибку "_context не существует")
        public Index_CoachModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public Coach? CurrentCoach { get; set; }
        public List<Training> UpcomingTrainings { get; set; } = new List<Training>();
        public int TodayTrainingsCount { get; set; }
        public string AverageAttendance { get; set; } = "0%";
        public int TotalStudents { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Получаем ID авторизованного тренера из Claims
            var coachIdStr = User.FindFirst("CoachId")?.Value;

            if (int.TryParse(coachIdStr, out int coachId))
            {
                // Загружаем данные текущего тренера
                CurrentCoach = await _context.Coaches.FirstOrDefaultAsync(c => c.CoachId == coachId);

                var today = DateOnly.FromDateTime(DateTime.Today);

                // Ищем в БД только те тренировки, которые привязаны к этому CoachId и будут сегодня или позже
                UpcomingTrainings = await _context.Training
                    .Include(t => t.Team)
                        .ThenInclude(team => team.Students)
                    .Include(t => t.Facility)
                    .Where(t => t.CoachId == coachId && t.DateTraining >= today)
                    .OrderBy(t => t.DateTraining).ThenBy(t => t.TimeTraining)
                    .Take(5) // Берем 5 ближайших
                    .ToListAsync();

                // Считаем тренировки именно на сегодня
                TodayTrainingsCount = UpcomingTrainings.Count(t => t.DateTraining == today);

                // Расчет средней посещаемости по прошлым тренировкам этого тренера
                var pastTrainings = await _context.Training
                    .Where(t => t.CoachId == coachId && t.DateTraining < today)
                    .Select(t => t.TrainingId)
                    .ToListAsync();

                if (pastTrainings.Any())
                {
                    var attendances = await _context.Attendances
                        .Where(a => pastTrainings.Contains(a.TrainingId))
                        .ToListAsync();

                    if (attendances.Any())
                    {
                        int present = attendances.Count(a => a.StatusAttendance == "Был");
                        AverageAttendance = Math.Round((double)present / attendances.Count * 100) + "%";
                    }
                }

                // Количество уникальных учеников в группах тренера
                var teamIds = await _context.Training
                    .Where(t => t.CoachId == coachId)
                    .Select(t => t.TeamId)
                    .Distinct()
                    .ToListAsync();

                TotalStudents = await _context.Students
                    .Where(s => s.TeamId.HasValue && teamIds.Contains(s.TeamId.Value))
                    .CountAsync();
            }
            else
            {
                // Если CoachId не найден, отправляем на страницу входа
                return RedirectToPage("/Login");
            }

            return Page();
        }
    }
}