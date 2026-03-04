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
    public class CoachesModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public CoachesModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class CoachListDto
        {
            public int CoachId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public string Specialty { get; set; } = string.Empty;
            public string Qualification { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
            public string StatusClass { get; set; } = string.Empty;
        }

        public List<CoachListDto> CoachesList { get; set; } = new List<CoachListDto>();
        public List<string> Specialties { get; set; } = new List<string>();

        [BindProperty]
        public Coach NewCoach { get; set; } = new Coach();

        public async Task OnGetAsync()
        {
            var coaches = await _context.Coaches
                .Include(c => c.Training)
                .ToListAsync();

            Specialties = coaches
                .Where(c => !string.IsNullOrEmpty(c.SpecialtyCoach))
                .Select(c => c.SpecialtyCoach)
                .Distinct()
                .ToList();

            foreach (var c in coaches)
            {
                var activeGroups = c.Training.Select(t => t.TeamId).Distinct().Count();
                string surnameInitial = string.IsNullOrEmpty(c.SurnameCoach) ? "" : c.SurnameCoach[0].ToString();
                string nameInitial = string.IsNullOrEmpty(c.NameCoach) ? "" : c.NameCoach[0].ToString();

                CoachesList.Add(new CoachListDto
                {
                    CoachId = c.CoachId,
                    FullName = $"{c.SurnameCoach} {c.NameCoach}",
                    Initials = (surnameInitial + nameInitial).ToUpper(),
                    Specialty = c.SpecialtyCoach,
                    Qualification = c.QualificationCoach,
                    StatusText = activeGroups > 0 ? "Занят" : "Свободен",
                    StatusClass = activeGroups > 0 ? "status-busy" : "status-free"
                });
            }
        }

        public async Task<IActionResult> OnPostAddCoachAsync()
        {
            try
            {
                _context.Coaches.Add(NewCoach);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Тренер {NewCoach.SurnameCoach} {NewCoach.NameCoach} успешно добавлен!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении тренера: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToPage();
        }
    }
}