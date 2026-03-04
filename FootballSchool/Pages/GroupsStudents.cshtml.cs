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
    public class GroupsStudentsModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public GroupsStudentsModel(FootballSchoolContext context)
        {
            _context = context;
        }
        public class TeamDto
        {
            public int TeamId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int StudentsCount { get; set; }
            public string CoachName { get; set; } = string.Empty;
        }

        public class StudentDto
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public int Age { get; set; }
            public string ParentName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string TeamName { get; set; } = string.Empty;
            public int ProgressPercentage { get; set; }
        }

        public List<TeamDto> Teams { get; set; } = new List<TeamDto>();
        public List<StudentDto> Students { get; set; } = new List<StudentDto>();

        [BindProperty]
        public Team NewTeam { get; set; } = new Team();

        [BindProperty]
        public Student NewStudent { get; set; } = new Student();

        public SelectList TeamSelectList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var teamsData = await _context.Teams
                .Include(t => t.Students)
                .Include(t => t.Training)
                    .ThenInclude(tr => tr.Coach)
                .ToListAsync();

            TeamSelectList = new SelectList(teamsData, "TeamId", "CategoryTeam");

            foreach (var t in teamsData)
            {
                var coach = t.Training.FirstOrDefault()?.Coach;
                string coachName = coach != null ? $"{coach.SurnameCoach} {coach.NameCoach[0]}." : "Не назначен";

                Teams.Add(new TeamDto
                {
                    TeamId = t.TeamId,
                    CategoryName = t.CategoryTeam,
                    Status = t.StatusTeam,
                    StudentsCount = t.Students.Count,
                    CoachName = coachName
                });
            }
            var studentsData = await _context.Students
                .Include(s => s.Team)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var random = new Random();

            foreach (var s in studentsData)
            {
                var age = today.Year - s.BirthStudent.Year;
                if (s.BirthStudent > today.AddYears(-age)) age--;

                Students.Add(new StudentDto
                {
                    StudentId = s.StudentId,
                    FullName = $"{s.SurnameStudent} {s.NameStudent}",
                    Age = age,
                    ParentName = $"{s.SurnameParent} {s.NameParent}",
                    Phone = s.ParentNumber ?? "Не указан",
                    TeamName = s.Team?.CategoryTeam ?? "Без группы",
                    ProgressPercentage = random.Next(40, 95)
                });
            }
        }
        public async Task<IActionResult> OnPostAddTeamAsync()
        {
            try
            {
                _context.Teams.Add(NewTeam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Группа «{NewTeam.CategoryTeam}» успешно добавлена!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении группы: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStudentAsync()
        {
            try
            {
                _context.Students.Add(NewStudent);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ученик {NewStudent.SurnameStudent} {NewStudent.NameStudent} успешно добавлен!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении ученика: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }
    }
}