using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FootballSchool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FootballSchool.Pages.Main_Pages
{
    [Authorize(Roles = "Parent")]
    public class Index_ParentModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public Index_ParentModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public Student StudentData { get; set; }
        public List<Training> UpcomingTrainings { get; set; }
        public Subscription ActiveSubscription { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToPage("/Login");
            }

            // Загружаем данные ученика
            StudentData = await _context.Students
                .Include(s => s.Team)
                .Include(s => s.Subscriptions)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (StudentData != null)
            {
                if (StudentData.TeamId.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    UpcomingTrainings = await _context.Training
                        .Include(t => t.Coach)
                        .Include(t => t.Facility)
                        .Where(t => t.TeamId == StudentData.TeamId.Value && t.DateTraining >= today)
                        .OrderBy(t => t.DateTraining)
                        .ThenBy(t => t.TimeTraining)
                        .Take(4)
                        .ToListAsync();
                }

                // Получаем последний оформленный абонемент
                ActiveSubscription = StudentData.Subscriptions
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefault();
            }

            return Page();
        }
    }
}