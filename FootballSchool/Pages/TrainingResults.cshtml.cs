using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class TrainingResultsModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public TrainingResultsModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class StudentProgressDto
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Initials { get; set; } = string.Empty;
            public int Age { get; set; }
            public string TeamName { get; set; } = string.Empty;

            public string LatestTestName { get; set; } = "Нет данных";
            public string LatestTestDate { get; set; } = "-";
            public string LatestTestResult { get; set; } = "-";

            public int ProgressPercentage { get; set; }
        }

        public List<StudentProgressDto> StudentsList { get; set; } = new();

        public string StudentsHistoryJson { get; set; } = "{}";

        [BindProperty]
        public int SelectedStudentId { get; set; }

        [BindProperty]
        public int EditingResultId { get; set; }

        [BindProperty]
        public string TestType { get; set; } = "Скорость";
        [BindProperty]
        public DateTime TestDate { get; set; } = DateTime.Today;
        [BindProperty]
        public string TestResult { get; set; } = string.Empty;
        [BindProperty]
        public string ResultUnit { get; set; } = "сек";
        [BindProperty]
        public string? TestNotes { get; set; }
        [BindProperty]
        public string? CoachComments { get; set; }

        public async Task OnGetAsync()
        {
            var students = await _context.Students
                .Include(s => s.Team)
                .Include(s => s.Progresses)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var random = new Random();
            var historyDict = new Dictionary<int, List<object>>();

            foreach (var s in students)
            {
                var age = today.Year - s.BirthStudent.Year;
                if (s.BirthStudent > today.AddYears(-age)) age--;

                string initials = $"{(string.IsNullOrEmpty(s.NameStudent) ? "" : s.NameStudent[0].ToString())}{(string.IsNullOrEmpty(s.SurnameStudent) ? "" : s.SurnameStudent[0].ToString())}";

                var studentHistory = new List<object>();
                foreach (var p in s.Progresses.OrderBy(p => p.DateProgress))
                {
                    string type = "Тест";
                    string valStr = "0";
                    double val = 0;
                    string unit = "";

                    if (!string.IsNullOrEmpty(p.TestsProgress))
                    {
                        var parts = p.TestsProgress.Split('|');
                        if (parts.Length == 2)
                        {
                            type = parts[0];
                            var valParts = parts[1].Split(' ', 2);
                            valStr = valParts[0];
                            unit = valParts.Length > 1 ? valParts[1] : "";

                            //Принудительный парсинг чисел, понимающий любой формат (с точкой или запятой)
                            double.TryParse(valStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out val);
                        }
                    }

                    studentHistory.Add(new
                    {
                        id = p.ProgressId,
                        date = p.DateProgress.ToString("dd.MM.yyyy"),
                        type = type,
                        value = val,
                        unit = unit,
                        note = p.PlanProgress ?? "",
                        comment = p.CommentProgress ?? ""
                    });
                }
                historyDict[s.StudentId] = studentHistory;

                var latestProgress = s.Progresses.OrderByDescending(p => p.DateProgress).FirstOrDefault();
                string testName = "Нет тестов";
                string testDate = "-";
                string testResultStr = "-";

                if (latestProgress != null && !string.IsNullOrEmpty(latestProgress.TestsProgress))
                {
                    var parts = latestProgress.TestsProgress.Split('|');
                    if (parts.Length == 2)
                    {
                        testName = parts[0];
                        testResultStr = parts[1];
                    }
                    else
                    {
                        testName = "Тест";
                        testResultStr = latestProgress.TestsProgress;
                    }
                    testDate = latestProgress.DateProgress.ToString("dd.MM.yyyy");
                }

                StudentsList.Add(new StudentProgressDto
                {
                    StudentId = s.StudentId,
                    FullName = $"{s.SurnameStudent} {s.NameStudent}",
                    Initials = initials.ToUpper(),
                    Age = age,
                    TeamName = s.Team?.CategoryTeam ?? "Без группы",
                    LatestTestName = testName,
                    LatestTestDate = testDate,
                    LatestTestResult = testResultStr,
                    ProgressPercentage = random.Next(40, 95)
                });
            }

            StudentsHistoryJson = JsonSerializer.Serialize(historyDict);
        }

        public async Task<IActionResult> OnPostSaveResultAsync()
        {
            if (SelectedStudentId == 0)
            {
                TempData["ErrorMessage"] = "Ошибка: не выбран ученик!";
                return RedirectToPage();
            }

            try
            {
                var cleanResult = TestResult.Replace(",", ".");

                if (EditingResultId > 0)
                {
                    // Редактирование существующего результата
                    var progress = await _context.Progresses.FindAsync(EditingResultId);
                    if (progress != null)
                    {
                        progress.DateProgress = DateOnly.FromDateTime(TestDate);
                        progress.TestsProgress = $"{TestType}|{cleanResult} {ResultUnit}";
                        progress.PlanProgress = TestNotes;
                        progress.CommentProgress = CoachComments;

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Результаты тестирования успешно обновлены!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Ошибка: результат не найден!";
                    }
                }
                else
                {
                    // Создание нового результата
                    var progress = new Progress
                    {
                        StudentId = SelectedStudentId,
                        DateProgress = DateOnly.FromDateTime(TestDate),
                        TestsProgress = $"{TestType}|{cleanResult} {ResultUnit}",
                        PlanProgress = TestNotes,
                        CommentProgress = CoachComments
                    };

                    _context.Progresses.Add(progress);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Результаты тестирования успешно добавлены!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка сохранения: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteResultAsync(int resultId)
        {
            try
            {
                var progress = await _context.Progresses.FindAsync(resultId);
                if (progress != null)
                {
                    _context.Progresses.Remove(progress);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Результат тестирования успешно удален!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Ошибка: результат не найден!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при удалении: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }
    }
}