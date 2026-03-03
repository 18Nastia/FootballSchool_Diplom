using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class AttendanceModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public AttendanceModel(FootballSchoolContext context)
        {
            _context = context;
        }

        // Список групп для фильтра
        public SelectList TeamList { get; set; } = default!;

        // Выбранные фильтры
        [BindProperty(SupportsGet = true)]
        public int? FilterTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        // Идентификатор конкретно выбранной тренировки из списка
        [BindProperty(SupportsGet = true)]
        public int? SelectedTrainingId { get; set; }

        // Доступные тренировки на выбранный день
        public List<Training> AvailableTrainings { get; set; } = new List<Training>();

        // Данные для формы сохранения
        [BindProperty]
        public List<AttendanceItem> AttendanceItems { get; set; } = new List<AttendanceItem>();

        public class AttendanceItem
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string Status { get; set; } = "Был";
            public string? Comment { get; set; }
        }

        public string TrainingInfo { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var date = FilterDate ?? DateTime.Today;
            FilterDate = date;

            var teams = await _context.Teams.ToListAsync();
            TeamList = new SelectList(teams, "TeamId", "CategoryTeam");

            if (!FilterTeamId.HasValue && teams.Any())
            {
                FilterTeamId = teams.First().TeamId;
            }

            if (FilterTeamId.HasValue)
            {
                var dateOnly = DateOnly.FromDateTime(date);

                // 1. Ищем ВСЕ тренировки этой группы в выбранный день
                AvailableTrainings = await _context.Training
                    .Include(t => t.Coach)
                    .Include(t => t.Facility)
                    .Where(t => t.TeamId == FilterTeamId && t.DateTraining == dateOnly)
                    .OrderBy(t => t.TimeTraining)
                    .ToListAsync();

                // Если тренировка всего одна и мы еще ничего не выбрали - выбираем её автоматически
                if (AvailableTrainings.Count == 1 && !SelectedTrainingId.HasValue)
                {
                    SelectedTrainingId = AvailableTrainings.First().TrainingId;
                }
                // Если мы сменили день/группу, а старый SelectedTrainingId остался и он не из этого списка
                else if (SelectedTrainingId.HasValue && !AvailableTrainings.Any(t => t.TrainingId == SelectedTrainingId.Value))
                {
                    SelectedTrainingId = null;
                }

                // 2. Если конкретная тренировка выбрана, загружаем для неё посещаемость
                if (SelectedTrainingId.HasValue)
                {
                    var training = AvailableTrainings.First(t => t.TrainingId == SelectedTrainingId.Value);

                    var coachName = training.Coach != null ? $"{training.Coach.SurnameCoach} {training.Coach.NameCoach[0]}." : "Без тренера";
                    var facilityName = training.Facility != null ? training.Facility.NameFacility : "Зал не указан";
                    var plan = string.IsNullOrEmpty(training.PlanTraining) ? "Тренировка" : training.PlanTraining;

                    TrainingInfo = $"{training.TimeTraining.ToString("HH:mm")} — {plan} ({facilityName} | Тренер: {coachName})";

                    var students = await _context.Students
                        .Where(s => s.TeamId == FilterTeamId)
                        .OrderBy(s => s.SurnameStudent)
                        .ToListAsync();

                    var existingAttendances = await _context.Attendances
                        .Where(a => a.TrainingId == SelectedTrainingId.Value)
                        .ToDictionaryAsync(a => a.StudentId);

                    foreach (var student in students)
                    {
                        var item = new AttendanceItem
                        {
                            StudentId = student.StudentId,
                            StudentName = $"{student.SurnameStudent} {student.NameStudent}"
                        };

                        if (existingAttendances.TryGetValue(student.StudentId, out var existingRecord))
                        {
                            item.Status = existingRecord.StatusAttendance;
                            item.Comment = existingRecord.NoteAttendance;
                        }

                        AttendanceItems.Add(item);
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!SelectedTrainingId.HasValue || AttendanceItems == null || !AttendanceItems.Any())
            {
                return RedirectToPage(new { FilterTeamId, FilterDate = FilterDate?.ToString("yyyy-MM-dd"), SelectedTrainingId });
            }

            foreach (var item in AttendanceItems)
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.TrainingId == SelectedTrainingId.Value && a.StudentId == item.StudentId);

                if (existing != null)
                {
                    existing.StatusAttendance = item.Status ?? "Был";
                    existing.NoteAttendance = item.Comment;
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        TrainingId = SelectedTrainingId.Value,
                        StudentId = item.StudentId,
                        StatusAttendance = item.Status ?? "Был",
                        NoteAttendance = item.Comment
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Посещаемость успешно сохранена!";

            return RedirectToPage(new { FilterTeamId, FilterDate = FilterDate?.ToString("yyyy-MM-dd"), SelectedTrainingId });
        }
    }
}