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
                // 1. Сохраняем тренера первым, чтобы получить сгенерированный БД CoachId
                _context.Coaches.Add(NewCoach);
                await _context.SaveChangesAsync();

                // 2. Генерируем сложный пароль (длина 12 символов)
                var password = GenerateComplexPassword(12);

                // 3. Формируем логин: coach_имя_айди (убираем пробелы и переводим в нижний регистр)
                string safeName = NewCoach.NameCoach?.Replace(" ", "").ToLower() ?? "coach";
                var login = $"coach_{safeName}_{NewCoach.CoachId}";

                var newUser = new User
                {
                    Login = login,
                    Password = password,
                    Role = "Coach"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Тренер {NewCoach.SurnameCoach} {NewCoach.NameCoach} успешно добавлен! Данные для входа: Логин - {login}, Пароль - {password}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении тренера: " + (ex.InnerException?.Message ?? ex.Message);
            }
            return RedirectToPage();
        }

        // Вспомогательный метод для генерации сложного пароля
        private string GenerateComplexPassword(int length)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new char[length];

            // Гарантируем наличие хотя бы одного символа из каждой обязательной группы
            password[0] = lower[random.Next(lower.Length)];
            password[1] = upper[random.Next(upper.Length)];
            password[2] = number[random.Next(number.Length)];
            password[3] = special[random.Next(special.Length)];

            // Заполняем оставшиеся символы случайным образом
            const string allChars = lower + upper + number + special;
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Перемешиваем символы для непредсказуемости
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }
}