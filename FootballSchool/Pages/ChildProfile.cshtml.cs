using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _environment;

        public ChildProfileModel(FootballSchoolContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public class ScheduleDto
        {
            public int Id { get; set; }
            public string DayOfWeek { get; set; } = string.Empty;
            public int DayOfWeekNumber { get; set; }
            public string Time { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
        }

        public class ResultDto
        {
            public int Id { get; set; }
            public string Date { get; set; } = string.Empty;
            public string MetricName { get; set; } = string.Empty;
            public string Score { get; set; } = string.Empty;
            public string Comment { get; set; } = string.Empty;
        }

        public class AchievementDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string IconClass { get; set; } = "fas fa-medal";
            public string? PhotoPath { get; set; }
            public bool IsManual { get; set; }
        }

        public class ProfileDto
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public string PhotoPath { get; set; } = string.Empty;
            public int Age { get; set; }
            public string TeamName { get; set; } = string.Empty;
            public string CoachName { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string ParentName { get; set; } = string.Empty;
            public string ParentPhone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;

            public List<ScheduleDto> Schedule { get; set; } = new List<ScheduleDto>();
            public List<ResultDto> Results { get; set; } = new List<ResultDto>();
            public List<AchievementDto> Achievements { get; set; } = new List<AchievementDto>();
        }

        public class NewAchievementModel
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime Date { get; set; } = DateTime.Today;
        }

        public ProfileDto StudentProfile { get; set; } = default!;

        [BindProperty]
        public Student EditStudent { get; set; } = new Student();

        [BindProperty]
        public Progress NewProgress { get; set; } = new Progress();

        [BindProperty]
        public NewAchievementModel NewAchievement { get; set; } = new NewAchievementModel();

        [BindProperty]
        public IFormFile? StudentPhoto { get; set; }

        public SelectList TeamList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue && User.IsInRole("Parent"))
            {
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdStr, out int uid))
                {
                    var parentStudent = await _context.Students.FirstOrDefaultAsync(s => s.UserId == uid);
                    if (parentStudent != null) id = parentStudent.StudentId;
                }
            }

            if (!id.HasValue) return Page();

            var student = await _context.Students
                .Include(s => s.Team)
                    .ThenInclude(t => t.Training)
                        .ThenInclude(tr => tr.Coach)
                .Include(s => s.Team)
                    .ThenInclude(t => t.Training)
                        .ThenInclude(tr => tr.Facility)
                .Include(s => s.Progresses)
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.StudentId == id.Value);

            if (student == null) return Page();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - student.BirthStudent.Year;
            if (student.BirthStudent > today.AddYears(-age)) age--;

            var initials = $"{(string.IsNullOrEmpty(student.NameStudent) ? "" : student.NameStudent[0].ToString())}{(string.IsNullOrEmpty(student.SurnameStudent) ? "" : student.SurnameStudent[0].ToString())}";
            var coach = student.Team?.Training.Select(t => t.Coach).FirstOrDefault();

            var culture = new CultureInfo("ru-RU");

            var scheduleList = new List<ScheduleDto>();
            if (student.Team?.Training != null)
            {
                scheduleList = student.Team.Training
                    .Where(t => t.DateTraining >= today)
                    .OrderBy(t => t.DateTraining)
                    .ThenBy(t => t.TimeTraining)
                    .Take(6)
                    .Select(t => new ScheduleDto
                    {
                        Id = t.TrainingId,
                        DayOfWeek = culture.DateTimeFormat.GetDayName(t.DateTraining.DayOfWeek),
                        Date = t.DateTraining.ToString("dd.MM.yyyy"),
                        Time = t.TimeTraining.ToString("HH:mm"),
                        Location = t.Facility?.NameFacility ?? "Основное поле"
                    }).ToList();
            }

            var resultsList = student.Progresses?
                .Where(p => string.IsNullOrEmpty(p.TestsProgress) || !p.TestsProgress.StartsWith("ACHIEVEMENT|"))
                .OrderByDescending(p => p.DateProgress)
                .Select(p =>
                {
                    string mName = "Тест";
                    string score = "-";
                    if (!string.IsNullOrEmpty(p.TestsProgress))
                    {
                        var parts = p.TestsProgress.Split('|');
                        if (parts.Length == 2)
                        {
                            mName = parts[0];
                            score = parts[1];
                        }
                        else
                        {
                            score = p.TestsProgress;
                        }
                    }

                    return new ResultDto
                    {
                        Id = p.ProgressId,
                        Date = p.DateProgress.ToString("dd MMM yyyy", culture),
                        MetricName = mName,
                        Score = score,
                        Comment = p.CommentProgress ?? p.PlanProgress ?? "Без комментариев"
                    };
                }).ToList() ?? new List<ResultDto>();

            var achievementsList = new List<AchievementDto>();

            int attendancesCount = student.Attendances?.Count(a => a.StatusAttendance == "Был") ?? 0;
            if (attendancesCount > 0)
                achievementsList.Add(new AchievementDto
                {
                    Title = "Первый шаг",
                    Description = "Успешно посетил первую тренировку!",
                    Date = "Выполнено",
                    IconClass = "fas fa-shoe-prints text-success",
                    IsManual = false
                });

            if (attendancesCount >= 10)
                achievementsList.Add(new AchievementDto
                {
                    Title = "Стабильность",
                    Description = $"Посетил {attendancesCount} тренировок",
                    Date = "Выполнено",
                    IconClass = "fas fa-fire text-danger",
                    IsManual = false
                });

            if (student.LevelStudent == "Продвинутый" || student.LevelStudent == "Профи")
                achievementsList.Add(new AchievementDto
                {
                    Title = "Элитный статус",
                    Description = $"Достиг уровня: {student.LevelStudent}",
                    Date = "Выполнено",
                    IconClass = "fas fa-star text-warning",
                    IsManual = false
                });

            var manualAchievements = student.Progresses?
                .Where(p => !string.IsNullOrEmpty(p.TestsProgress) && p.TestsProgress.StartsWith("ACHIEVEMENT|"))
                .OrderByDescending(p => p.DateProgress)
                .Select(p => new AchievementDto
                {
                    Id = p.ProgressId,
                    Title = p.TestsProgress.Split('|').Length > 1 ? p.TestsProgress.Split('|')[1] : "Достижение",
                    Description = p.PlanProgress ?? "",
                    Date = p.DateProgress.ToString("dd.MM.yyyy"),
                    PhotoPath = p.CommentProgress,
                    IsManual = true,
                    IconClass = "fas fa-award text-primary"
                }).ToList() ?? new List<AchievementDto>();

            achievementsList.AddRange(manualAchievements);

            StudentProfile = new ProfileDto
            {
                StudentId = student.StudentId,
                FullName = $"{student.SurnameStudent} {student.NameStudent} {student.MiddleStudent}".Trim(),
                Initials = initials.ToUpper(),
                PhotoPath = student.PhotoStudent ?? "",
                Age = age,
                TeamName = student.Team?.CategoryTeam ?? "Без группы",
                CoachName = coach != null ? $"{coach.SurnameCoach} {coach.NameCoach}" : "Не назначен",
                Level = student.LevelStudent,
                ParentName = $"{student.SurnameParent} {student.NameParent} {student.MiddleParent}".Trim(),
                ParentPhone = student.ParentNumber,
                Address = $"{student.CityStudent}, ул. {student.StreetStudent}, д. {student.HouseStudent}{(string.IsNullOrEmpty(student.ApartmentStudent) ? "" : $", кв. {student.ApartmentStudent}")}",
                Schedule = scheduleList,
                Results = resultsList,
                Achievements = achievementsList
            };

            EditStudent = student;
            TeamList = new SelectList(await _context.Teams.ToListAsync(), "TeamId", "CategoryTeam");

            return Page();
        }

        public async Task<IActionResult> OnPostAddAchievementAsync(int studentId, IFormFile? AchievementPhoto)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            string? photoPath = null;

            if (AchievementPhoto != null && AchievementPhoto.Length > 0)
            {
                string imgext = Path.GetExtension(AchievementPhoto.FileName).ToLower();
                if (imgext == ".jpg" || imgext == ".jpeg" || imgext == ".png" || imgext == ".gif" || imgext == ".webp")
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + imgext;
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "achievements");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await AchievementPhoto.CopyToAsync(stream);
                    }

                    photoPath = "/images/achievements/" + uniqueFileName;
                }
            }

            var progress = new Progress
            {
                StudentId = studentId,
                DateProgress = DateOnly.FromDateTime(NewAchievement.Date),
                TestsProgress = "ACHIEVEMENT|" + NewAchievement.Title,
                PlanProgress = NewAchievement.Description,
                CommentProgress = photoPath
            };

            _context.Progresses.Add(progress);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Достижение успешно добавлено!";

            return RedirectToPage(new { id = studentId });
        }

        public async Task<IActionResult> OnPostAddProgressAsync(int studentId)
        {
            if (ModelState.IsValid)
            {
                NewProgress.StudentId = studentId;
                _context.Progresses.Add(NewProgress);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Запись успешно добавлена!";
            }

            return RedirectToPage(new { id = studentId });
        }

        public async Task<IActionResult> OnPostDeleteProgressAsync(int progressId, int studentId)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var progress = await _context.Progresses.FindAsync(progressId);
            if (progress != null)
            {
                if (!string.IsNullOrEmpty(progress.CommentProgress) && progress.TestsProgress?.StartsWith("ACHIEVEMENT|") == true)
                {
                    var filePath = Path.Combine(_environment.WebRootPath, progress.CommentProgress.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Progresses.Remove(progress);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Запись/Достижение успешно удалено!";
            }

            return RedirectToPage(new { id = studentId });
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

                if (StudentPhoto != null && StudentPhoto.Length > 0)
                {
                    string extension = Path.GetExtension(StudentPhoto.FileName).ToLower();
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                    if (allowedExtensions.Contains(extension))
                    {
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "students");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        if (!string.IsNullOrEmpty(existing.PhotoStudent))
                        {
                            string oldFilePath = Path.Combine(_environment.WebRootPath, existing.PhotoStudent.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + extension;
                        string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await StudentPhoto.CopyToAsync(stream);
                        }

                        existing.PhotoStudent = "/images/students/" + uniqueFileName;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Данные ученика успешно обновлены!";
            }

            return RedirectToPage(new { id = EditStudent.StudentId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/AccessDenied");

            var student = await _context.Students
                .Include(s => s.Attendances)
                .Include(s => s.Progresses)
                .Include(s => s.Subscriptions)
                    .ThenInclude(sub => sub.Payments)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Ученик не найден.";
                return RedirectToPage("/GroupsStudents");
            }

            try
            {
                var userId = student.UserId;

                if (!string.IsNullOrEmpty(student.PhotoStudent))
                {
                    string studentPhotoPath = Path.Combine(_environment.WebRootPath, student.PhotoStudent.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(studentPhotoPath))
                    {
                        System.IO.File.Delete(studentPhotoPath);
                    }
                }

                if (student.Subscriptions != null && student.Subscriptions.Any())
                {
                    foreach (var subscription in student.Subscriptions)
                    {
                        if (subscription.Payments != null && subscription.Payments.Any())
                        {
                            _context.Payments.RemoveRange(subscription.Payments);
                        }
                    }

                    _context.Subscriptions.RemoveRange(student.Subscriptions);
                }

                if (student.Progresses != null && student.Progresses.Any())
                {
                    foreach (var progress in student.Progresses)
                    {
                        if (!string.IsNullOrEmpty(progress.CommentProgress) && progress.TestsProgress?.StartsWith("ACHIEVEMENT|") == true)
                        {
                            string achievementPhotoPath = Path.Combine(_environment.WebRootPath, progress.CommentProgress.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(achievementPhotoPath))
                            {
                                System.IO.File.Delete(achievementPhotoPath);
                            }
                        }
                    }

                    _context.Progresses.RemoveRange(student.Progresses);
                }

                if (student.Attendances != null && student.Attendances.Any())
                    _context.Attendances.RemoveRange(student.Attendances);

                _context.Students.Remove(student);

                if (userId.HasValue)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ученик и его аккаунт успешно удалены.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при удалении ученика: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage("/GroupsStudents");
        }
    }
}