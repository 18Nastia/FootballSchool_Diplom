using ClosedXML.Excel;
using FootballSchool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FootballSchool.Pages
{
    public class AttendanceModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public AttendanceModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public SelectList TeamList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int? FilterTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedTrainingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "Daily";

        [BindProperty(SupportsGet = true)]
        public string ReportPeriod { get; set; } = "month";

        public List<Training> AvailableTrainings { get; set; } = new List<Training>();
        public HashSet<int> FilledTrainingIds { get; set; } = new HashSet<int>();
        public bool IsAlreadyFilled { get; set; }

        [BindProperty]
        public List<AttendanceItem> AttendanceItems { get; set; } = new List<AttendanceItem>();

        public class AttendanceItem
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string Status { get; set; } = "Был";
            public string? Comment { get; set; }
        }

        public class StudentStatDto
        {
            public string StudentName { get; set; } = string.Empty;
            public int TotalTrainings { get; set; }
            public int Attended { get; set; }
            public int Absent { get; set; }
            public int Percentage { get; set; }
        }

        public List<StudentStatDto> ReportStats { get; set; } = new List<StudentStatDto>();
        public string TrainingInfo { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
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
            TeamList = new SelectList(teams, "TeamId", "CategoryTeam");

            if (!FilterTeamId.HasValue && teams.Any())
            {
                FilterTeamId = teams.First().TeamId;
            }

            if (ViewMode == "Daily")
            {
                var date = FilterDate ?? DateTime.Today;
                FilterDate = date;

                if (FilterTeamId.HasValue)
                {
                    var dateOnly = DateOnly.FromDateTime(date);

                    var trainingsQuery = _context.Training
                        .Include(t => t.Coach)
                        .Include(t => t.Facility)
                        .Where(t => t.TeamId == FilterTeamId && t.DateTraining == dateOnly);

                    if (User.IsInRole("Coach"))
                    {
                        var coachIdStr = User.FindFirst("CoachId")?.Value;
                        if (int.TryParse(coachIdStr, out int coachId))
                        {
                            trainingsQuery = trainingsQuery.Where(t => t.CoachId == coachId);
                        }
                    }

                    AvailableTrainings = await trainingsQuery.OrderBy(t => t.TimeTraining).ToListAsync();

                    var trainingIds = AvailableTrainings.Select(t => t.TrainingId).ToList();
                    var filledIds = await _context.Attendances
                        .Where(a => trainingIds.Contains(a.TrainingId))
                        .Select(a => a.TrainingId)
                        .Distinct()
                        .ToListAsync();

                    FilledTrainingIds = new HashSet<int>(filledIds);

                    if (AvailableTrainings.Count == 1 && !SelectedTrainingId.HasValue)
                    {
                        SelectedTrainingId = AvailableTrainings.First().TrainingId;
                    }
                    else if (SelectedTrainingId.HasValue && !AvailableTrainings.Any(t => t.TrainingId == SelectedTrainingId.Value))
                    {
                        SelectedTrainingId = null;
                    }

                    if (SelectedTrainingId.HasValue)
                    {
                        IsAlreadyFilled = FilledTrainingIds.Contains(SelectedTrainingId.Value);
                        var training = AvailableTrainings.First(t => t.TrainingId == SelectedTrainingId.Value);

                        var coachName = training.Coach != null ? $"{training.Coach.SurnameCoach} {training.Coach.NameCoach[0]}." : "Без тренера";
                        var facilityName = training.Facility != null ? training.Facility.NameFacility : "Зал не указан";
                        var plan = string.IsNullOrEmpty(training.PlanTraining) ? "Тренировка" : training.PlanTraining;

                        TrainingInfo = $"{training.TimeTraining.ToString("HH:mm")} — {plan} ({facilityName} | Тренер: {coachName})";

                        var students = await _context.Students
                            .Where(s => s.TeamId == FilterTeamId)
                            .OrderBy(s => s.SurnameStudent)
                            .ToListAsync();

                        var existingAttendances = await _context.Attendances
                            .Where(a => a.TrainingId == SelectedTrainingId.Value)
                            .ToDictionaryAsync(a => a.StudentId);

                        foreach (var student in students)
                        {
                            var item = new AttendanceItem
                            {
                                StudentId = student.StudentId,
                                StudentName = $"{student.SurnameStudent} {student.NameStudent}"
                            };

                            if (existingAttendances.TryGetValue(student.StudentId, out var existingRecord))
                            {
                                item.Status = existingRecord.StatusAttendance;
                                item.Comment = existingRecord.NoteAttendance;
                            }

                            AttendanceItems.Add(item);
                        }
                    }
                }
            }
            else if (ViewMode == "Report" && FilterTeamId.HasValue)
            {
                DateTime startDate = DateTime.Today;
                if (ReportPeriod == "week") startDate = DateTime.Today.AddDays(-7);
                else if (ReportPeriod == "month") startDate = DateTime.Today.AddMonths(-1);
                else if (ReportPeriod == "year") startDate = DateTime.Today.AddYears(-1);
                else startDate = DateTime.MinValue;

                var dateOnlyStart = DateOnly.FromDateTime(startDate);
                var dateOnlyEnd = DateOnly.FromDateTime(DateTime.Today);

                var pastTrainings = await _context.Training
                    .Where(t => t.TeamId == FilterTeamId && t.DateTraining >= dateOnlyStart && t.DateTraining <= dateOnlyEnd)
                    .Select(t => t.TrainingId)
                    .ToListAsync();

                int totalTrainings = pastTrainings.Count;

                var studentsInTeam = await _context.Students
                    .Where(s => s.TeamId == FilterTeamId)
                    .OrderBy(s => s.SurnameStudent)
                    .ToListAsync();

                var attendances = await _context.Attendances
                    .Where(a => pastTrainings.Contains(a.TrainingId))
                    .ToListAsync();

                foreach (var st in studentsInTeam)
                {
                    var stAtt = attendances.Where(a => a.StudentId == st.StudentId).ToList();
                    int attended = stAtt.Count(a => a.StatusAttendance == "Был");
                    int absent = totalTrainings - attended;

                    int percentage = totalTrainings > 0 ? (int)Math.Round((double)attended / totalTrainings * 100) : 0;

                    ReportStats.Add(new StudentStatDto
                    {
                        StudentName = $"{st.SurnameStudent} {st.NameStudent}",
                        TotalTrainings = totalTrainings,
                        Attended = attended,
                        Absent = absent,
                        Percentage = percentage
                    });
                }

                ReportStats = ReportStats.OrderByDescending(x => x.Percentage).ToList();
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!SelectedTrainingId.HasValue || AttendanceItems == null || !AttendanceItems.Any())
            {
                return RedirectToPage(new { FilterTeamId, FilterDate = FilterDate?.ToString("yyyy-MM-dd"), SelectedTrainingId, ViewMode = "Daily" });
            }

            foreach (var item in AttendanceItems)
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.TrainingId == SelectedTrainingId.Value && a.StudentId == item.StudentId);

                if (existing != null)
                {
                    existing.StatusAttendance = item.Status ?? "Был";
                    existing.NoteAttendance = item.Comment;
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        TrainingId = SelectedTrainingId.Value,
                        StudentId = item.StudentId,
                        StatusAttendance = item.Status ?? "Был",
                        NoteAttendance = item.Comment
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Посещаемость успешно сохранена!";

            return RedirectToPage(new { FilterTeamId, FilterDate = FilterDate?.ToString("yyyy-MM-dd"), SelectedTrainingId, ViewMode = "Daily" });
        }

        // --- ЭКСПОРТ В EXCEL ДЛЯ СТАТИСТИКИ ---
        public async Task<IActionResult> OnGetExportReportAsync(int teamId, string period)
        {
            DateTime startDate = DateTime.Today;
            if (period == "week") startDate = DateTime.Today.AddDays(-7);
            else if (period == "month") startDate = DateTime.Today.AddMonths(-1);
            else if (period == "year") startDate = DateTime.Today.AddYears(-1);
            else startDate = DateTime.MinValue;

            var dateOnlyStart = DateOnly.FromDateTime(startDate);
            var dateOnlyEnd = DateOnly.FromDateTime(DateTime.Today);

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound();

            var pastTrainings = await _context.Training
                .Where(t => t.TeamId == teamId && t.DateTraining >= dateOnlyStart && t.DateTraining <= dateOnlyEnd)
                .Select(t => t.TrainingId)
                .ToListAsync();

            int totalTrainings = pastTrainings.Count;

            var students = await _context.Students
                .Where(s => s.TeamId == teamId).OrderBy(s => s.SurnameStudent).ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => pastTrainings.Contains(a.TrainingId)).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Статистика");

                worksheet.Cell(1, 1).Value = $"Отчет по группе: {team.CategoryTeam}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;

                worksheet.Cell(3, 1).Value = "Ученик";
                worksheet.Cell(3, 2).Value = "Всего занятий";
                worksheet.Cell(3, 3).Value = "Присутствовал";
                worksheet.Cell(3, 4).Value = "Пропустил";
                worksheet.Cell(3, 5).Value = "% Посещаемости";
                worksheet.Range("A3:E3").Style.Font.Bold = true;
                worksheet.Range("A3:E3").Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 4;
                foreach (var st in students)
                {
                    var stAtt = attendances.Where(a => a.StudentId == st.StudentId).ToList();
                    int attended = stAtt.Count(a => a.StatusAttendance == "Был");
                    int absent = totalTrainings - attended;
                    int percentage = totalTrainings > 0 ? (int)Math.Round((double)attended / totalTrainings * 100) : 0;

                    worksheet.Cell(row, 1).Value = $"{st.SurnameStudent} {st.NameStudent}";
                    worksheet.Cell(row, 2).Value = totalTrainings;
                    worksheet.Cell(row, 3).Value = attended;
                    worksheet.Cell(row, 4).Value = absent;
                    worksheet.Cell(row, 5).Value = percentage + "%";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Посещаемость_{team.CategoryTeam}_{DateTime.Now:dd.MM.yyyy}.xlsx");
                }
            }
        }
    }
}