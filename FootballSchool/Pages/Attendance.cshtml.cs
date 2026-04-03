using ClosedXML.Excel;
using FootballSchool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public class AttendanceJournalRow
        {
            public int StudentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public List<string> Marks { get; set; } = new();
        }

        public List<Training> JournalTrainings { get; set; } = new();
        public List<AttendanceJournalRow> JournalRows { get; set; } = new();
        public List<int> JournalTotals { get; set; } = new();

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

                    AvailableTrainings = await trainingsQuery
                        .OrderBy(t => t.TimeTraining)
                        .ToListAsync();

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

                        var coachName = training.Coach != null
                            ? $"{training.Coach.SurnameCoach} {training.Coach.NameCoach[0]}."
                            : "Без тренера";

                        var facilityName = training.Facility != null
                            ? training.Facility.NameFacility
                            : "Зал не указан";

                        var plan = string.IsNullOrEmpty(training.PlanTraining)
                            ? "Тренировка"
                            : training.PlanTraining;

                        TrainingInfo = $"{training.TimeTraining:HH:mm} — {plan} ({facilityName} | Тренер: {coachName})";

                        var students = await _context.Students
                            .Where(s => s.TeamId == FilterTeamId)
                            .OrderBy(s => s.SurnameStudent)
                            .ThenBy(s => s.NameStudent)
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

                JournalTrainings = await _context.Training
                    .Where(t => t.TeamId == FilterTeamId &&
                                t.DateTraining >= dateOnlyStart &&
                                t.DateTraining <= dateOnlyEnd)
                    .OrderBy(t => t.DateTraining)
                    .ThenBy(t => t.TimeTraining)
                    .ToListAsync();

                var studentsInTeam = await _context.Students
                    .Where(s => s.TeamId == FilterTeamId)
                    .OrderBy(s => s.SurnameStudent)
                    .ThenBy(s => s.NameStudent)
                    .ToListAsync();

                var trainingIds = JournalTrainings.Select(t => t.TrainingId).ToList();

                var attendances = await _context.Attendances
                    .Where(a => trainingIds.Contains(a.TrainingId))
                    .ToListAsync();

                foreach (var st in studentsInTeam)
                {
                    var row = new AttendanceJournalRow
                    {
                        StudentId = st.StudentId,
                        StudentName = $"{st.SurnameStudent} {st.NameStudent}"
                    };

                    foreach (var training in JournalTrainings)
                    {
                        var attendance = attendances.FirstOrDefault(a =>
                            a.StudentId == st.StudentId &&
                            a.TrainingId == training.TrainingId);

                        if (attendance == null)
                            row.Marks.Add("");
                        else if (attendance.StatusAttendance == "Был")
                            row.Marks.Add("✔");
                        else
                            row.Marks.Add("✖");
                    }

                    JournalRows.Add(row);
                }

                foreach (var training in JournalTrainings)
                {
                    var presentCount = attendances.Count(a =>
                        a.TrainingId == training.TrainingId &&
                        a.StatusAttendance == "Был");

                    JournalTotals.Add(presentCount);
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!SelectedTrainingId.HasValue || AttendanceItems == null || !AttendanceItems.Any())
            {
                return RedirectToPage(new
                {
                    FilterTeamId,
                    FilterDate = FilterDate?.ToString("yyyy-MM-dd"),
                    SelectedTrainingId,
                    ViewMode = "Daily"
                });
            }

            foreach (var item in AttendanceItems)
            {
                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a =>
                        a.TrainingId == SelectedTrainingId.Value &&
                        a.StudentId == item.StudentId);

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

            return RedirectToPage(new
            {
                FilterTeamId,
                FilterDate = FilterDate?.ToString("yyyy-MM-dd"),
                SelectedTrainingId,
                ViewMode = "Daily"
            });
        }

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

            var trainings = await _context.Training
                .Where(t => t.TeamId == teamId &&
                            t.DateTraining >= dateOnlyStart &&
                            t.DateTraining <= dateOnlyEnd)
                .OrderBy(t => t.DateTraining)
                .ThenBy(t => t.TimeTraining)
                .ToListAsync();

            var students = await _context.Students
                .Where(s => s.TeamId == teamId)
                .OrderBy(s => s.SurnameStudent)
                .ThenBy(s => s.NameStudent)
                .ToListAsync();

            var trainingIds = trainings.Select(t => t.TrainingId).ToList();

            var attendances = await _context.Attendances
                .Where(a => trainingIds.Contains(a.TrainingId))
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Посещаемость");

                worksheet.Cell(1, 1).Value = $"Журнал посещаемости: {team.CategoryTeam}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;

                worksheet.Cell(3, 1).Value = "ФИО";
                for (int i = 0; i < trainings.Count; i++)
                {
                    worksheet.Cell(3, i + 2).Value = $"{trainings[i].DateTraining:dd.MM} {trainings[i].TimeTraining:HH\\:mm}";
                }

                worksheet.Range(3, 1, 3, trainings.Count + 1).Style.Font.Bold = true;
                worksheet.Range(3, 1, 3, trainings.Count + 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                int rowIndex = 4;
                foreach (var st in students)
                {
                    worksheet.Cell(rowIndex, 1).Value = $"{st.SurnameStudent} {st.NameStudent}";

                    for (int i = 0; i < trainings.Count; i++)
                    {
                        var attendance = attendances.FirstOrDefault(a =>
                            a.StudentId == st.StudentId &&
                            a.TrainingId == trainings[i].TrainingId);

                        string mark = "";
                        if (attendance == null)
                            mark = "";
                        else if (attendance.StatusAttendance == "Был")
                            mark = "✔";
                        else
                            mark = "✖";

                        worksheet.Cell(rowIndex, i + 2).Value = mark;
                    }

                    rowIndex++;
                }

                worksheet.Cell(rowIndex, 1).Value = "ИТОГО";
                worksheet.Cell(rowIndex, 1).Style.Font.Bold = true;

                for (int i = 0; i < trainings.Count; i++)
                {
                    var presentCount = attendances.Count(a =>
                        a.TrainingId == trainings[i].TrainingId &&
                        a.StatusAttendance == "Был");

                    worksheet.Cell(rowIndex, i + 2).Value = presentCount;
                    worksheet.Cell(rowIndex, i + 2).Style.Font.Bold = true;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Журнал_посещаемости_{team.CategoryTeam}_{DateTime.Now:dd.MM.yyyy}.xlsx"
                    );
                }
            }
        }
    }
}