using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace FootballSchool.Pages
{
    public class CoachesModel : PageModel
    {
        private readonly FootballSchoolContext _context;
        private readonly IWebHostEnvironment _env;

        public CoachesModel(FootballSchoolContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private static readonly string[] AllowedImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        private bool IsValidImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return true;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedImageExtensions.Contains(extension);
        }

        private string AllowedImageFormatsText()
        {
            return "Допустимы только файлы: .jpg, .jpeg, .png, .gif, .webp";
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
            public string PhotoPath { get; set; } = string.Empty;
        }

        public List<CoachListDto> CoachesList { get; set; } = new List<CoachListDto>();
        public List<string> Specialties { get; set; } = new List<string>();

        [BindProperty]
        public Coach NewCoach { get; set; } = new Coach();

        [BindProperty]
        public IFormFile? CoachPhotoUpload { get; set; }

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
                    StatusClass = activeGroups > 0 ? "status-busy" : "status-free",
                    PhotoPath = c.PhotoCoach ?? ""
                });
            }
        }

        public async Task<IActionResult> OnPostAddCoachAsync()
        {
            ModelState.Remove("NewCoach.User");
            ModelState.Remove("NewCoach.Training");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Пожалуйста, проверьте правильность заполнения формы.";
                return RedirectToPage();
            }

            if (!IsValidImage(CoachPhotoUpload))
            {
                TempData["ErrorMessage"] = AllowedImageFormatsText();
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (CoachPhotoUpload != null && CoachPhotoUpload.Length > 0)
                {
                    string wwwRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string uploadsFolder = Path.Combine(wwwRootPath, "uploads", "coaches");
                    Directory.CreateDirectory(uploadsFolder);

                    string extension = Path.GetExtension(CoachPhotoUpload.FileName).ToLowerInvariant();
                    string uniqueFileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await CoachPhotoUpload.CopyToAsync(fileStream);
                    }
                    NewCoach.PhotoCoach = "/uploads/coaches/" + uniqueFileName;
                }

                var password = GenerateComplexPassword(12);

                string safeName = NewCoach.NameCoach?.Replace(" ", "").ToLower() ?? "coach";
                string baseLogin = $"coach_{safeName}";
                string login = baseLogin;

                int counter = 1;
                while (await _context.Users.AnyAsync(u => u.Login == login))
                {
                    login = $"{baseLogin}{counter}";
                    counter++;
                }

                var newUser = new User
                {
                    Login = login,
                    Password = password,
                    Role = "Coach"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                NewCoach.UserId = newUser.UserId;
                _context.Coaches.Add(NewCoach);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Тренер {NewCoach.SurnameCoach} {NewCoach.NameCoach} успешно добавлен! Данные для входа: Логин - {login}, Пароль - {password}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Ошибка при добавлении тренера: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        private string GenerateComplexPassword(int length)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new char[length];

            password[0] = lower[random.Next(lower.Length)];
            password[1] = upper[random.Next(upper.Length)];
            password[2] = number[random.Next(number.Length)];
            password[3] = special[random.Next(special.Length)];

            const string allChars = lower + upper + number + special;
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        public async Task<IActionResult> OnPostDeleteCoachAsync(int id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/AccessDenied");

            var coach = await _context.Coaches
                .Include(c => c.Training)
                    .ThenInclude(t => t.Attendances)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach == null)
            {
                TempData["ErrorMessage"] = "Тренер не найден.";
                return RedirectToPage();
            }

            try
            {
                var userId = coach.UserId;

                if (!string.IsNullOrEmpty(coach.PhotoCoach))
                {
                    string coachPhotoPath = Path.Combine(_env.WebRootPath, coach.PhotoCoach.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(coachPhotoPath))
                    {
                        System.IO.File.Delete(coachPhotoPath);
                    }
                }

                if (coach.Training != null && coach.Training.Any())
                {
                    foreach (var tr in coach.Training)
                    {
                        if (tr.Attendances != null && tr.Attendances.Any())
                            _context.Attendances.RemoveRange(tr.Attendances);
                    }

                    _context.Training.RemoveRange(coach.Training);
                }

                _context.Coaches.Remove(coach);

                if (userId.HasValue)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                    if (user != null)
                        _context.Users.Remove(user);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тренер и его аккаунт успешно удалены.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при удалении тренера: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }
    }
}