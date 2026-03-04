using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class ChildProfileModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public ChildProfileModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class ProfileDto
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public int Age { get; set; }
            public string TeamName { get; set; } = string.Empty;
            public string CoachName { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string ParentName { get; set; } = string.Empty;
            public string ParentPhone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }

        public ProfileDto StudentProfile { get; set; } = default!;

        [BindProperty]
        public Student EditStudent { get; set; } = new Student();

        public SelectList TeamList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            
            if (!id.HasValue && User.IsInRole("Parent"))
            {
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdStr, out int uid))
                {
                    var parentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == uid);
                    if (parentStudent != null)
                    {
                        id = parentStudent.StudentId;
                    }
                }
            }

            if (!id.HasValue)
            {
                return Page();
            }

            var student = await _context.Students
                .Include(s => s.Team)
                    .ThenInclude(t => t.Training)
                        .ThenInclude(tr => tr.Coach)
                .FirstOrDefaultAsync(s => s.StudentId == id.Value);

            if (student == null)
            {
                return Page();
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - student.BirthStudent.Year;
            if (student.BirthStudent > today.AddYears(-age)) age--;

            var initials = $"{(string.IsNullOrEmpty(student.NameStudent) ? "" : student.NameStudent[0].ToString())}{(string.IsNullOrEmpty(student.SurnameStudent) ? "" : student.SurnameStudent[0].ToString())}";

            var coach = student.Team?.Training.Select(t => t.Coach).FirstOrDefault();

            StudentProfile = new ProfileDto
            {
                StudentId = student.StudentId,
                FullName = $"{student.SurnameStudent} {student.NameStudent} {student.MiddleStudent}".Trim(),
                Initials = initials.ToUpper(),
                Age = age,
                TeamName = student.Team?.CategoryTeam ?? "Без группы",
                CoachName = coach != null ? $"{coach.SurnameCoach} {coach.NameCoach}" : "Не назначен",
                Level = student.LevelStudent,
                ParentName = $"{student.SurnameParent} {student.NameParent} {student.MiddleParent}".Trim(),
                ParentPhone = student.ParentNumber,
                Address = $"{student.CityStudent}, ул. {student.StreetStudent}, д. {student.HouseStudent}{(string.IsNullOrEmpty(student.ApartmentStudent) ? "" : $", кв. {student.ApartmentStudent}")}"
            };

            EditStudent = student;
            TeamList = new SelectList(await _context.Teams.ToListAsync(), "TeamId", "CategoryTeam");

            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var existing = await _context.Students.FindAsync(EditStudent.StudentId);
            if (existing != null)
            {
                existing.NameStudent = EditStudent.NameStudent;
                existing.SurnameStudent = EditStudent.SurnameStudent;
                existing.MiddleStudent = EditStudent.MiddleStudent;
                existing.BirthStudent = EditStudent.BirthStudent;
                existing.GenderStudent = EditStudent.GenderStudent;
                existing.TeamId = EditStudent.TeamId;
                existing.LevelStudent = EditStudent.LevelStudent;
                existing.MedicalStudent = EditStudent.MedicalStudent;
                existing.SurnameParent = EditStudent.SurnameParent;
                existing.NameParent = EditStudent.NameParent;
                existing.MiddleParent = EditStudent.MiddleParent;
                existing.ParentNumber = EditStudent.ParentNumber;
                existing.CityStudent = EditStudent.CityStudent;
                existing.StreetStudent = EditStudent.StreetStudent;
                existing.HouseStudent = EditStudent.HouseStudent;
                existing.ApartmentStudent = EditStudent.ApartmentStudent;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Данные ученика успешно обновлены!";
            }
            return RedirectToPage(new { id = EditStudent.StudentId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var student = await _context.Students
                .Include(s => s.Attendances)
                .Include(s => s.Progresses)
                .Include(s => s.Subscriptions)
                    .ThenInclude(sub => sub.Payments)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student != null)
            {
                if (student.Subscriptions.Any())
                {
                    foreach (var sub in student.Subscriptions)
                    {
                        _context.Payments.RemoveRange(sub.Payments);
                    }
                    _context.Subscriptions.RemoveRange(student.Subscriptions);
                }
                if (student.Progresses.Any()) _context.Progresses.RemoveRange(student.Progresses);
                if (student.Attendances.Any()) _context.Attendances.RemoveRange(student.Attendances);

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ученик был полностью удален.";
            }
            return RedirectToPage("/GroupsStudents");
        }
    }
}