using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class SubscriptionsModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public SubscriptionsModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class SubDto
        {
            public int SubscriptionId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Terms { get; set; } = string.Empty;
            public int DaysCount { get; set; }
            public decimal Cost { get; set; }
            public string Status { get; set; } = "Активен";
            public string StatusClass { get; set; } = "badge-active";
        }

        public List<SubDto> SubscriptionsList { get; set; } = new();

        [BindProperty]
        public Subscription ModalSub { get; set; } = new();

        public SelectList StudentsList { get; set; } = default!;

        public int ActiveCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task OnGetAsync()
        {
            var subs = await _context.Subscriptions
                .Include(s => s.Student)
                .ToListAsync();

            var students = await _context.Students
                .Select(s => new { s.StudentId, FullName = $"{s.SurnameStudent} {s.NameStudent}" })
                .ToListAsync();

            StudentsList = new SelectList(students, "StudentId", "FullName");

            foreach (var s in subs)
            {
                string status = s.DaysSubscription > 0 ? "Активен" : "Истёк";
                string statusClass = s.DaysSubscription > 0 ? "badge-active" : "badge-expired";

                if (s.DaysSubscription > 0)
                {
                    ActiveCount++;
                    TotalRevenue += s.CostSubscription ?? 0;
                }

                SubscriptionsList.Add(new SubDto
                {
                    SubscriptionId = s.SubscriptionId,
                    StudentName = s.Student != null ? $"{s.Student.SurnameStudent} {s.Student.NameStudent}" : "Неизвестно",
                    Type = s.TypeSubscription,
                    Terms = s.TermsSubscription ?? "Стандартные",
                    DaysCount = s.DaysSubscription ?? 0,
                    Cost = s.CostSubscription ?? 0,
                    Status = status,
                    StatusClass = statusClass
                });
            }
        }

        public async Task<IActionResult> OnPostSaveSubAsync()
        {
            ModelState.Clear();

            try
            {
                if (ModalSub.SubscriptionId == 0)
                {
                    _context.Subscriptions.Add(ModalSub);
                    TempData["SuccessMessage"] = "Новый абонемент успешно выдан!";
                }
                else
                {
                    var existing = await _context.Subscriptions.FindAsync(ModalSub.SubscriptionId);
                    if (existing != null)
                    {
                        existing.StudentId = ModalSub.StudentId;
                        existing.TypeSubscription = ModalSub.TypeSubscription;
                        existing.TermsSubscription = ModalSub.TermsSubscription;
                        existing.DaysSubscription = ModalSub.DaysSubscription;
                        existing.CostSubscription = ModalSub.CostSubscription;
                        TempData["SuccessMessage"] = "Данные абонемента обновлены!";
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSubAsync(int id)
        {
            var sub = await _context.Subscriptions.Include(s => s.Payments).FirstOrDefaultAsync(s => s.SubscriptionId == id);
            if (sub != null)
            {
                if (sub.Payments.Any())
                {
                    _context.Payments.RemoveRange(sub.Payments);
                }
                _context.Subscriptions.Remove(sub);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Абонемент аннулирован.";
            }
            return RedirectToPage();
        }
    }
}