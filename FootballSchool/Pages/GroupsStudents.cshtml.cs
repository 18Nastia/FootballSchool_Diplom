using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        [BindProperty]
        public string? ParentEmail { get; set; }

        public SelectList TeamSelectList { get; set; } = default!;

        // Список филиалов для привязки к группе
        public SelectList BranchSelectList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            NewStudent.BirthStudent = new DateOnly(DateTime.Today.Year - 8, 1, 1);

            var teamsData = await _context.Teams
                .Include(t => t.Students)
                        .Include(t => t.Training)
                    .ThenInclude(tr => tr.Coach)
                .ToListAsync();

            TeamSelectList = new SelectList(teamsData, "TeamId", "CategoryTeam");

            // Загружаем филиалы
            var branchesData = await _context.Branches.ToListAsync();
            BranchSelectList = new SelectList(branchesData, "BranchId", "NameBranch");

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
            ModelState.Clear();
            TryValidateModel(NewTeam, nameof(NewTeam));

            if (!ModelState.IsValid)
            {
                var errs = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key.Replace("NewTeam.", "")}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Ошибка заполнения группы: " + string.Join(" | ", errs);
                return RedirectToPage();
            }

            try
            {
                _context.Teams.Add(NewTeam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Группа «{NewTeam.CategoryTeam}» успешно добавлена!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении группы: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStudentAsync()
        {
            ModelState.Clear();
            TryValidateModel(NewStudent, nameof(NewStudent));

            if (!ModelState.IsValid)
            {
                var errs = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key.Replace("NewStudent.", "")}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Ошибка заполнения ученика: " + string.Join(" | ", errs);
                return RedirectToPage();
            }

            try
            {
                // 1. Генерируем сложный пароль (длина 12 символов)
                var password = GenerateComplexPassword(12);

                // 2. Формируем логин: parent_фамилия_перваябукваимени_цифры с применением транслитерации
                string surname = Transliterate(NewStudent.SurnameParent);
                string firstLetter = Transliterate(!string.IsNullOrWhiteSpace(NewStudent.NameParent) ? NewStudent.NameParent.Substring(0, 1) : "x");
                string randomNum = new Random().Next(1000, 9999).ToString();

                var login = $"parent_{surname}_{firstLetter}_{randomNum}";

                var newUser = new User
                {
                    Login = login,
                    Password = password,
                    Role = "Parent",
                    Email = ParentEmail
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(); // Сохраняем, чтобы получить UserId

                // 3. Привязываем аккаунт к ученику
                NewStudent.UserId = newUser.UserId;

                _context.Students.Add(NewStudent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Ученик {NewStudent.SurnameStudent} {NewStudent.NameStudent} успешно добавлен! Данные для входа родителя: Логин - {login}, Пароль - {password}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при добавлении ученика: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        // Вспомогательный метод для транслитерации кириллицы в латиницу
        private string Transliterate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "parent";

            var dict = new Dictionary<char, string>
            {
                {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"}, {'е', "e"}, {'ё', "e"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
                {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"}, {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
                {'у', "u"}, {'ф', "f"}, {'х', "h"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "shch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
                {'э', "e"}, {'ю', "yu"}, {'я', "ya"}
            };

            var result = new StringBuilder();
            foreach (var ch in text.ToLower())
            {
                if (dict.ContainsKey(ch))
                    result.Append(dict[ch]);
                else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
                    result.Append(ch);
            }

            var finalString = result.ToString();
            return string.IsNullOrEmpty(finalString) ? "parent" : finalString;
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