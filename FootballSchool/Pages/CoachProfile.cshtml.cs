using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class CoachProfileModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public CoachProfileModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class CoachProfileDto
        {
            public int CoachId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public string Specialty { get; set; } = string.Empty;
            public string Qualification { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
            public string StatusClass { get; set; } = string.Empty;
            public string Schedule { get; set; } = string.Empty;
            public string Salary { get; set; } = string.Empty;
            public List<CoachGroupDto> Groups { get; set; } = new List<CoachGroupDto>();
            public List<CoachAwardDto> Awards { get; set; } = new List<CoachAwardDto>();
        }

        public class CoachGroupDto
        {
            public string CategoryName { get; set; } = string.Empty;
            public int StudentsCount { get; set; }
            public string ScheduleInfo { get; set; } = string.Empty;
        }

        public class CoachAwardDto
        {
            public string Title { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string IconClass { get; set; } = "fas fa-medal";
        }

        public CoachProfileDto Profile { get; set; } = default!;

        [BindProperty]
        public Coach EditCoach { get; set; } = new Coach();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var coach = await _context.Coaches
                .Include(c => c.Training)
                    .ThenInclude(t => t.Team)
                        .ThenInclude(team => team.Students)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach == null) return NotFound();

            // 1. ДИНАМИЧЕСКИЕ ГРУППЫ ТРЕНЕРА
            // Выбираем только те команды, для которых у тренера есть будущие/текущие тренировки,
            // либо если их нет - вообще все исторические группы.
            var today = DateOnly.FromDateTime(DateTime.Today);
            var activeTrainings = coach.Training.Where(t => t.Team != null && t.DateTraining >= today).ToList();

            // Если нет будущих тренировок, показываем все закрепленные группы из истории
            if (!activeTrainings.Any())
            {
                activeTrainings = coach.Training.Where(t => t.Team != null).ToList();
            }

            var groupedTeams = activeTrainings.GroupBy(t => t.Team).ToList();
            var groupsDto = new List<CoachGroupDto>();

            var culture = new System.Globalization.CultureInfo("ru-RU");

            foreach (var g in groupedTeams)
            {
                var team = g.Key;
                // Собираем уникальные дни недели для этой группы
                var days = g.Select(tr => culture.DateTimeFormat.GetAbbreviatedDayName(tr.DateTraining.DayOfWeek)).Distinct();
                string daysStr = string.Join(", ", days);
                var firstTime = g.OrderBy(tr => tr.TimeTraining).FirstOrDefault()?.TimeTraining.ToString("HH:mm") ?? "";

                groupsDto.Add(new CoachGroupDto
                {
                    CategoryName = team.CategoryTeam,
                    StudentsCount = team.Students.Count,
                    ScheduleInfo = string.IsNullOrEmpty(daysStr) ? "Расписание уточняется" : $"{daysStr} в {firstTime}"
                });
            }

            // 2. ДИНАМИЧЕСКИЕ НАГРАДЫ / ДОСТИЖЕНИЯ
            var awardsList = new List<CoachAwardDto>();

            if (!string.IsNullOrWhiteSpace(coach.QualificationCoach))
            {
                awardsList.Add(new CoachAwardDto
                {
                    Title = "Квалификация",
                    Description = coach.QualificationCoach,
                    Date = "Подтверждено",
                    IconClass = "fas fa-certificate text-primary"
                });
            }

            if (!string.IsNullOrWhiteSpace(coach.SpecialtyCoach))
            {
                awardsList.Add(new CoachAwardDto
                {
                    Title = "Специализация",
                    Description = coach.SpecialtyCoach,
                    Date = "Профиль",
                    IconClass = "fas fa-star text-warning"
                });
            }

            int trainingCount = coach.Training.Count;
            if (trainingCount > 0)
            {
                string countTitle = trainingCount > 20 ? "Легендарный наставник" : (trainingCount >= 5 ? "Опытный тренер" : "Начало пути");
                string icon = trainingCount > 20 ? "fas fa-crown text-warning" : (trainingCount >= 5 ? "fas fa-trophy text-success" : "fas fa-seedling text-info");

                awardsList.Add(new CoachAwardDto
                {
                    Title = countTitle,
                    Description = $"Проведено тренировок в школе: {trainingCount}",
                    Date = "Достижение",
                    IconClass = icon
                });
            }

            string surnameInitial = string.IsNullOrEmpty(coach.SurnameCoach) ? "" : coach.SurnameCoach[0].ToString();
            string nameInitial = string.IsNullOrEmpty(coach.NameCoach) ? "" : coach.NameCoach[0].ToString();

            Profile = new CoachProfileDto
            {
                CoachId = coach.CoachId,
                FullName = $"{coach.SurnameCoach} {coach.NameCoach} {coach.MiddleCoach}".Trim(),
                Initials = (surnameInitial + nameInitial).ToUpper(),
                Specialty = coach.SpecialtyCoach,
                Qualification = coach.QualificationCoach,
                StatusText = groupsDto.Any() ? "Занят (ведет группы)" : "Свободен",
                StatusClass = groupsDto.Any() ? "status-busy" : "status-free",
                Schedule = coach.ScheduleCoach ?? "Не указан",
                Salary = coach.SalaryCoach?.ToString("N0") ?? "Не указана",
                Groups = groupsDto,
                Awards = awardsList
            };

            EditCoach = coach;
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var existing = await _context.Coaches.FindAsync(EditCoach.CoachId);
            if (existing != null)
            {
                existing.SurnameCoach = EditCoach.SurnameCoach;
                existing.NameCoach = EditCoach.NameCoach;
                existing.MiddleCoach = EditCoach.MiddleCoach;
                existing.SpecialtyCoach = EditCoach.SpecialtyCoach;
                existing.QualificationCoach = EditCoach.QualificationCoach;
                existing.ScheduleCoach = EditCoach.ScheduleCoach;
                existing.SalaryCoach = EditCoach.SalaryCoach;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Профиль тренера успешно обновлен!";
            }
            return RedirectToPage(new { id = EditCoach.CoachId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var coach = await _context.Coaches
                .Include(c => c.Training)
                    .ThenInclude(t => t.Attendances)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach != null)
            {
                if (coach.Training.Any())
                {
                    foreach (var tr in coach.Training)
                    {
                        _context.Attendances.RemoveRange(tr.Attendances);
                    }
                    _context.Training.RemoveRange(coach.Training);
                }
                _context.Coaches.Remove(coach);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тренер был удален.";
            }
            return RedirectToPage("/Coaches");
        }
    }
}