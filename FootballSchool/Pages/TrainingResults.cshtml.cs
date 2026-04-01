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
using ClosedXML.Excel;
using System.IO;

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

        // Добавляем свойства для фильтрации по группе
        [BindProperty(SupportsGet = true)]
        public int? FilterTeamId { get; set; }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList TeamList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Загружаем список групп для выпадающего списка (с учетом прав тренера)
            var teamsQuery = _context.Teams.AsQueryable();
            if (User.IsInRole("Coach"))
            {
                var coachIdStr = User.FindFirst("CoachId")?.Value;
                if (int.TryParse(coachIdStr, out int coachId))
                {
                    var coachTeamIds = await _context.Training
                        .Where(t => t.CoachId == coachId)
                        .Select(t => t.TeamId)
                        .Distinct()
                        .ToListAsync();
                    teamsQuery = teamsQuery.Where(t => coachTeamIds.Contains(t.TeamId));
                }
            }
            var teams = await teamsQuery.ToListAsync();
            TeamList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(teams, "TeamId", "CategoryTeam");

            var studentsQuery = _context.Students
                .Include(s => s.Team)
                .Include(s => s.Progresses)
                .Include(s => s.Attendances)
                    .ThenInclude(a => a.Training)
                .AsQueryable();

            if (User.IsInRole("Coach"))
            {
                var coachIdStr = User.FindFirst("CoachId")?.Value;
                if (int.TryParse(coachIdStr, out int coachId))
                {
                    var coachTeamIds = await _context.Training
                        .Where(t => t.CoachId == coachId)
                        .Select(t => t.TeamId)
                        .Distinct()
                        .ToListAsync();

                    studentsQuery = studentsQuery.Where(s => s.TeamId.HasValue && coachTeamIds.Contains(s.TeamId.Value));
                }
            }

            // Применяем фильтр по группе, если он выбран
            if (FilterTeamId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.TeamId == FilterTeamId.Value);
            }

            var students = await studentsQuery.ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
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

                // ЛОГИКА РАСЧЕТА ПРОГРЕССА НА ОСНОВЕ ПОСЕЩАЕМОСТИ ЗА МЕСЯЦ
                int progressPercentage = 0;
                var monthAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));

                var recentAttendances = s.Attendances
                    .Where(a => a.Training != null && a.Training.DateTraining >= monthAgo)
                    .ToList();

                if (recentAttendances.Any())
                {
                    int attendedCount = recentAttendances.Count(a => a.StatusAttendance == "Был");
                    progressPercentage = (int)Math.Round((double)attendedCount / recentAttendances.Count * 100);
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
                    ProgressPercentage = progressPercentage
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

        // --- ЭКСПОРТ В EXCEL ---
        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Результаты тестов");

                worksheet.Cell(1, 1).Value = "Ученик";
                worksheet.Cell(1, 2).Value = "Группа";
                worksheet.Cell(1, 3).Value = "Возраст";
                worksheet.Cell(1, 4).Value = "Последний тест";
                worksheet.Cell(1, 5).Value = "Дата теста";
                worksheet.Cell(1, 6).Value = "Результат";
                worksheet.Cell(1, 7).Value = "Дисциплина (Прогресс %)";

                worksheet.Range("A1:G1").Style.Font.Bold = true;
                worksheet.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.LightBlue;

                int row = 2;
                foreach (var student in StudentsList)
                {
                    worksheet.Cell(row, 1).Value = student.FullName;
                    worksheet.Cell(row, 2).Value = student.TeamName;
                    worksheet.Cell(row, 3).Value = student.Age;
                    worksheet.Cell(row, 4).Value = student.LatestTestName;
                    worksheet.Cell(row, 5).Value = student.LatestTestDate;
                    worksheet.Cell(row, 6).Value = student.LatestTestResult;
                    worksheet.Cell(row, 7).Value = student.ProgressPercentage + "%";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string groupName = FilterTeamId.HasValue ? $"_Группа_{FilterTeamId.Value}" : "_Все";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Сводка_тестов{groupName}_{DateTime.Now:dd.MM.yyyy}.xlsx");
                }
            }
        }

        // --- ДЕТАЛЬНЫЙ ЭКСПОРТ (ВСЯ ИСТОРИЯ ТЕСТОВ ГРУППЫ ИЛИ УЧЕНИКА) ---
        public async Task<IActionResult> OnGetExportDetailedAsync(int? teamId, int? studentId)
        {
            var progressesQuery = _context.Progresses
                .Include(p => p.Student)
                    .ThenInclude(s => s.Team)
                .AsQueryable();

            string reportName = "История_результатов";

            // Если передан ID ученика - фильтруем по нему
            if (studentId.HasValue)
            {
                progressesQuery = progressesQuery.Where(p => p.StudentId == studentId.Value);
                var student = await _context.Students.FindAsync(studentId.Value);
                if (student != null)
                {
                    reportName += $"_{student.SurnameStudent}_{student.NameStudent}";
                }
            }
            // Если передан ID группы - фильтруем по ней
            else if (teamId.HasValue)
            {
                progressesQuery = progressesQuery.Where(p => p.Student.TeamId == teamId.Value);
                var team = await _context.Teams.FindAsync(teamId.Value);
                if (team != null)
                {
                    reportName += $"_Группа_{team.CategoryTeam}";
                }
            }

            // Ограничения доступа для тренера
            if (User.IsInRole("Coach"))
            {
                var coachIdStr = User.FindFirst("CoachId")?.Value;
                if (int.TryParse(coachIdStr, out int coachId))
                {
                    var coachTeamIds = await _context.Training
                        .Where(t => t.CoachId == coachId)
                        .Select(t => t.TeamId)
                        .Distinct()
                        .ToListAsync();

                    progressesQuery = progressesQuery.Where(p => p.Student.TeamId.HasValue && coachTeamIds.Contains(p.Student.TeamId.Value));
                }
            }

            var progresses = await progressesQuery.OrderByDescending(p => p.DateProgress).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Результаты");

                worksheet.Cell(1, 1).Value = "Дата";
                worksheet.Cell(1, 2).Value = "Группа";
                worksheet.Cell(1, 3).Value = "Ученик";
                worksheet.Cell(1, 4).Value = "Дисциплина (Тест)";
                worksheet.Cell(1, 5).Value = "Результат";
                worksheet.Cell(1, 6).Value = "Примечание";
                worksheet.Cell(1, 7).Value = "Комментарий тренера";

                worksheet.Range("A1:G1").Style.Font.Bold = true;
                worksheet.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var p in progresses)
                {
                    string testType = "Тест";
                    string testValue = p.TestsProgress ?? "";

                    if (!string.IsNullOrEmpty(p.TestsProgress))
                    {
                        var parts = p.TestsProgress.Split('|');
                        if (parts.Length == 2)
                        {
                            testType = parts[0];
                            testValue = parts[1];
                        }
                    }

                    worksheet.Cell(row, 1).Value = p.DateProgress.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 2).Value = p.Student?.Team?.CategoryTeam ?? "Без группы";
                    worksheet.Cell(row, 3).Value = $"{p.Student?.SurnameStudent} {p.Student?.NameStudent}";
                    worksheet.Cell(row, 4).Value = testType;
                    worksheet.Cell(row, 5).Value = testValue;
                    worksheet.Cell(row, 6).Value = p.PlanProgress ?? "";
                    worksheet.Cell(row, 7).Value = p.CommentProgress ?? "";

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    reportName = reportName.Replace(" ", "_");

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportName}_{DateTime.Now:dd_MM_yyyy}.xlsx");
                }
            }
        }
    }
}