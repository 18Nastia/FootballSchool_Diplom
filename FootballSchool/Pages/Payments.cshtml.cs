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
    public class PaymentsModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public PaymentsModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class PaymentDto
        {
            public int PaymentId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string SubscriptionInfo { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Method { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public List<PaymentDto> PaymentsList { get; set; } = new();

        [BindProperty]
        public Payment ModalPayment { get; set; } = new();

        public SelectList SubscriptionsList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var payments = await _context.Payments
                .Include(p => p.Subscription)
                    .ThenInclude(s => s.Student)
                .OrderByDescending(p => p.DatePayment)
                .ToListAsync();

            // Для добавления платежа нужно выбрать к какому абонементу он относится
            var activeSubs = await _context.Subscriptions
                .Include(s => s.Student)
                .Select(s => new {
                    s.SubscriptionId,
                    DisplayText = $"{(s.Student != null ? s.Student.SurnameStudent : "")} - {s.TypeSubscription} ({s.CostSubscription} ₽)"
                })
                .ToListAsync();

            SubscriptionsList = new SelectList(activeSubs, "SubscriptionId", "DisplayText");

            foreach (var p in payments)
            {
                PaymentsList.Add(new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    StudentName = p.Subscription?.Student != null ? $"{p.Subscription.Student.SurnameStudent} {p.Subscription.Student.NameStudent}" : "Неизвестно",
                    SubscriptionInfo = p.Subscription?.TypeSubscription ?? "Неизвестный абонемент",
                    Amount = p.AmountPayment,
                    Date = new DateTime(p.DatePayment.Year, p.DatePayment.Month, p.DatePayment.Day),
                    Method = p.MethodPayment,
                    Status = p.StatusPayment ?? "В обработке"
                });
            }
        }

        public async Task<IActionResult> OnPostSavePaymentAsync()
        {
            ModelState.Clear();
            try
            {
                if (ModalPayment.PaymentId == 0)
                {
                    _context.Payments.Add(ModalPayment);
                    TempData["SuccessMessage"] = "Платеж успешно зарегистрирован!";
                }
                else
                {
                    var existing = await _context.Payments.FindAsync(ModalPayment.PaymentId);
                    if (existing != null)
                    {
                        existing.StatusPayment = ModalPayment.StatusPayment;
                        existing.MethodPayment = ModalPayment.MethodPayment;
                        existing.AmountPayment = ModalPayment.AmountPayment;
                        existing.DatePayment = ModalPayment.DatePayment;
                        TempData["SuccessMessage"] = "Статус платежа обновлен!";
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

        public async Task<IActionResult> OnPostDeletePaymentAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Запись о платеже удалена.";
            }
            return RedirectToPage();
        }
    }
}