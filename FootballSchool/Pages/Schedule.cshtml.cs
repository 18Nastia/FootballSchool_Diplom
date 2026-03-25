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
    public class ScheduleModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public ScheduleModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public IList<Training> Trainings { get; set; } = default!;

        [BindProperty]
        public Training ModalTraining { get; set; } = new Training();

        public SelectList BranchList { get; set; } = default!;
        public SelectList TeamList { get; set; } = default!;
        public SelectList CoachList { get; set; } = default!;
        public SelectList FacilityList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int? FilterBranchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CurrentDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewType { get; set; } = "week";

        public DateTime StartOfWeek { get; set; }
        public DateTime EndOfWeek { get; set; }

        public async Task OnGetAsync()
        {
            if (_context.Training == null) return;

            DateTime date = CurrentDate ?? DateTime.Today;
            CurrentDate = date;

            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            StartOfWeek = date.AddDays(-1 * diff);
            EndOfWeek = StartOfWeek.AddDays(6);

            await PopulateSelectListsAsync();
            await LoadTrainingsAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync(bool isBulkAdd)
        {
            // Проверка прав: только администратор может добавлять или изменять расписание
            if (!User.IsInRole("Admin")) return RedirectToPage("/AccessDenied");

            // Очистка навигационных свойств от ошибок валидации, чтобы не блокировать сохранение
            ModelState.Remove("ModalTraining.Facility");
            ModelState.Remove("ModalTraining.Coach");
            ModelState.Remove("ModalTraining.Team");
            ModelState.Remove("ModalTraining.Attendances");

            try
            {
                // Проверка совпадения филиалов группы и площадки
                var team = await _context.Teams.FindAsync(ModalTraining.TeamId);
                var facility = await _context.Facilities.FindAsync(ModalTraining.FacilityId);

                if (team != null && facility != null && team.BranchId != facility.BranchId)
                {
                    TempData["ErrorMessage"] = "Ошибка: Выбранная группа и площадка должны относиться к одному филиалу.";
                    return RedirectToPage(new { FilterBranchId, FilterTeamId, CurrentDate = CurrentDate?.ToString("yyyy-MM-dd"), ViewType });
                }

                // Добавление предупреждения, если тренировка создается/переносится на прошедшую дату
                var today = DateOnly.FromDateTime(DateTime.Today);

                if (ModalTraining.DateTraining < today)
                {
                    TempData["WarningMessage"] = "Внимание: Тренировка сохранена на прошедшую дату!";
                }
                else
                {
                    TempData["SuccessMessage"] = "Тренировка успешно сохранена!";
                }

                if (ModalTraining.TrainingId == 0)
                {
                    var newTraining = new Training
                    {
                        DateTraining = ModalTraining.DateTraining,
                        TimeTraining = ModalTraining.TimeTraining,
                        CoachId = ModalTraining.CoachId,
                        TeamId = ModalTraining.TeamId,
                        FacilityId = ModalTraining.FacilityId,
                        PlanTraining = ModalTraining.PlanTraining
                    };
                    _context.Training.Add(newTraining);
                }
                else
                {
                    var existingTraining = await _context.Training.FindAsync(ModalTraining.TrainingId);
                    if (existingTraining != null)
                    {
                        existingTraining.DateTraining = ModalTraining.DateTraining;
                        existingTraining.TimeTraining = ModalTraining.TimeTraining;
                        existingTraining.CoachId = ModalTraining.CoachId;
                        existingTraining.TeamId = ModalTraining.TeamId;
                        existingTraining.FacilityId = ModalTraining.FacilityId;
                        existingTraining.PlanTraining = ModalTraining.PlanTraining;
                    }
                }

                await _context.SaveChangesAsync();

                if (isBulkAdd)
                {
                    TempData["ShowAddModal"] = true;
                    TempData["BulkDate"] = ModalTraining.DateTraining.ToString("yyyy-MM-dd");
                    TempData["BulkTeamId"] = ModalTraining.TeamId.ToString();
                    TempData["BulkFacilityId"] = ModalTraining.FacilityId.ToString();
                    TempData["BulkCoachId"] = ModalTraining.CoachId.ToString();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка сохранения: " + (ex.InnerException?.Message ?? ex.Message);
                Console.WriteLine("Ошибка сохранения: " + ex.Message);
            }

            return RedirectToPage(new
            {
                FilterBranchId,
                FilterTeamId,
                CurrentDate = CurrentDate?.ToString("yyyy-MM-dd"),
                ViewType
            });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // Проверка прав: только администратор может удалять тренировки
            if (!User.IsInRole("Admin")) return RedirectToPage("/AccessDenied");

            var training = await _context.Training.FindAsync(id);
            if (training != null)
            {
                var relatedAttendances = _context.Attendances.Where(a => a.TrainingId == id);
                _context.Attendances.RemoveRange(relatedAttendances);

                _context.Training.Remove(training);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тренировка успешно удалена.";
            }

            return RedirectToPage(new
            {
                FilterBranchId,
                FilterTeamId,
                CurrentDate = CurrentDate?.ToString("yyyy-MM-dd"),
                ViewType
            });
        }

        private async Task PopulateSelectListsAsync()
        {
            var branches = await _context.Branches.ToListAsync();
            var teams = await _context.Teams.ToListAsync();
            var coaches = await _context.Coaches.Select(c => new { c.CoachId, FullName = c.SurnameCoach + " " + c.NameCoach }).ToListAsync();
            var facilities = await _context.Facilities.ToListAsync();

            BranchList = new SelectList(branches, "BranchId", "NameBranch");
            TeamList = new SelectList(teams, "TeamId", "CategoryTeam");
            CoachList = new SelectList(coaches, "CoachId", "FullName");
            FacilityList = new SelectList(facilities, "FacilityId", "NameFacility");
        }

        private async Task LoadTrainingsAsync()
        {
            var query = _context.Training
                .Include(t => t.Team)
                .Include(t => t.Coach)
                .Include(t => t.Facility)
                .AsQueryable();

            // Логика: Тренер видит только свои тренировки
            if (User.IsInRole("Coach"))
            {
                var coachIdStr = User.FindFirst("CoachId")?.Value;
                if (int.TryParse(coachIdStr, out int coachId))
                {
                    query = query.Where(t => t.CoachId == coachId);
                }
            }

            if (FilterTeamId.HasValue)
                query = query.Where(t => t.TeamId == FilterTeamId.Value);

            if (FilterBranchId.HasValue)
                query = query.Where(t => t.Facility.BranchId == FilterBranchId.Value);

            Trainings = await query.ToListAsync();
        }
    }
}