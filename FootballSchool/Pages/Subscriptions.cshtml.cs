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

            public string PaymentStatus { get; set; } = "Не оплачен";
            public string PaymentStatusClass { get; set; } = "error";
        }

        public class PaymentDto
        {
            public int PaymentId { get; set; }
            public int SubscriptionId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string SubscriptionInfo { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Method { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public List<SubDto> SubscriptionsList { get; set; } = new();
        public List<PaymentDto> PaymentsList { get; set; } = new();

        [BindProperty] public Subscription ModalSub { get; set; } = new();
        [BindProperty] public Payment ModalPayment { get; set; } = new();

        public SelectList StudentsList { get; set; } = default!;
        public SelectList ActiveSubscriptionsList { get; set; } = default!;

        public int ActiveCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<Subscription> query = _context.Subscriptions
                .Include(s => s.Student)
                .Include(s => s.Payments);

            IQueryable<Student> studentsQuery = _context.Students;
            IQueryable<Payment> paymentsQuery = _context.Payments.Include(p => p.Subscription).ThenInclude(s => s.Student);

            if (User.IsInRole("Parent"))
            {
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdStr, out int uid))
                {
                    query = query.Where(s => s.Student.UserId == uid);
                    studentsQuery = studentsQuery.Where(s => s.UserId == uid);
                    paymentsQuery = paymentsQuery.Where(p => p.Subscription.Student.UserId == uid);
                }
            }

            // 1. Загрузка абонементов
            var subs = await query.ToListAsync();
            var students = await studentsQuery.Select(s => new { s.StudentId, FullName = $"{s.SurnameStudent} {s.NameStudent}" }).ToListAsync();
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

                // Логика расчета статуса оплаты на основе связанных платежей
                decimal totalPaid = s.Payments?.Where(p => p.StatusPayment == "Оплачен").Sum(p => p.AmountPayment) ?? 0;
                decimal cost = s.CostSubscription ?? 0;

                string payStatus = "Не оплачен";
                string payClass = "error";

                if (totalPaid >= cost && cost > 0)
                {
                    payStatus = "Оплачен";
                    payClass = "success";
                }
                else if (totalPaid > 0)
                {
                    payStatus = $"Долг {cost - totalPaid} ₽";
                    payClass = "warning";
                }
                else if (cost == 0)
                {
                    payStatus = "Бесплатно";
                    payClass = "success";
                }
                else if (s.Payments != null && s.Payments.Any(p => p.StatusPayment == "В обработке"))
                {
                    // Если нет оплат, но есть платеж в обработке
                    payStatus = "В обработке";
                    payClass = "warning";
                }
                else if (s.Payments != null && s.Payments.Any(p => p.StatusPayment == "Ошибка"))
                {
                    // Если нет оплат и последний платеж выдал ошибку
                    payStatus = "Ошибка оплаты";
                    payClass = "error";
                }

                SubscriptionsList.Add(new SubDto
                {
                    SubscriptionId = s.SubscriptionId,
                    StudentName = s.Student != null ? $"{s.Student.SurnameStudent} {s.Student.NameStudent}" : "Неизвестно",
                    Type = s.TypeSubscription,
                    Terms = s.TermsSubscription ?? "Стандартные",
                    DaysCount = s.DaysSubscription ?? 0,
                    Cost = cost,
                    Status = status,
                    StatusClass = statusClass,
                    PaymentStatus = payStatus,
                    PaymentStatusClass = payClass 
                });
            }

            // 2. Загрузка платежей
            var payments = await paymentsQuery.OrderByDescending(p => p.DatePayment).ToListAsync();

            var activeSubs = await query.Select(s => new {
                s.SubscriptionId,
                DisplayText = $"{(s.Student != null ? s.Student.SurnameStudent : "")} - {s.TypeSubscription} ({s.CostSubscription} ₽)"
            }).ToListAsync();
            ActiveSubscriptionsList = new SelectList(activeSubs, "SubscriptionId", "DisplayText");

            foreach (var p in payments)
            {
                PaymentsList.Add(new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SubscriptionId = p.SubscriptionId,
                    StudentName = p.Subscription?.Student != null ? $"{p.Subscription.Student.SurnameStudent} {p.Subscription.Student.NameStudent}" : "Неизвестно",
                    SubscriptionInfo = p.Subscription?.TypeSubscription ?? "Неизвестный абонемент",
                    Amount = p.AmountPayment,
                    Date = new DateTime(p.DatePayment.Year, p.DatePayment.Month, p.DatePayment.Day),
                    Method = p.MethodPayment,
                    Status = p.StatusPayment ?? "В обработке"
                });
            }
        }

        public async Task<IActionResult> OnPostSaveSubAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();
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
                        _context.Subscriptions.Update(existing);
                        TempData["SuccessMessage"] = "Данные абонемента обновлены!";
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Ошибка: " + (ex.InnerException?.Message ?? ex.Message); }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSubAsync(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            var sub = await _context.Subscriptions.Include(s => s.Payments).FirstOrDefaultAsync(s => s.SubscriptionId == id);
            if (sub != null)
            {
                if (sub.Payments.Any()) _context.Payments.RemoveRange(sub.Payments);
                _context.Subscriptions.Remove(sub);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Абонемент аннулирован.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSavePaymentAsync()
        {
            if (!User.IsInRole("Admin") && ModalPayment.PaymentId != 0) return Forbid();
            ModelState.Clear();

            try
            {
                if (User.IsInRole("Parent"))
                {
                    ModalPayment.StatusPayment = "В обработке";
                    ModalPayment.MethodPayment = "Онлайн-эквайринг";
                    ModalPayment.DatePayment = DateOnly.FromDateTime(DateTime.Now);
                }

                if (ModalPayment.PaymentId == 0)
                {
                    _context.Payments.Add(ModalPayment);
                    TempData["SuccessMessage"] = User.IsInRole("Parent") ? "Заявка на оплату сформирована." : "Платеж зарегистрирован!";
                }
                else
                {
                    var existing = await _context.Payments.FindAsync(ModalPayment.PaymentId);
                    if (existing != null)
                    {
                        // Обновляем параметры И вызываем Update, чтобы контекст точно зафиксировал изменения
                        existing.StatusPayment = ModalPayment.StatusPayment;
                        existing.MethodPayment = ModalPayment.MethodPayment;
                        existing.AmountPayment = ModalPayment.AmountPayment;
                        if (ModalPayment.DatePayment != default) existing.DatePayment = ModalPayment.DatePayment;

                        _context.Payments.Update(existing);
                        TempData["SuccessMessage"] = "Статус платежа обновлен!";
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Ошибка сохранения: " + (ex.InnerException?.Message ?? ex.Message); }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeletePaymentAsync(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
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