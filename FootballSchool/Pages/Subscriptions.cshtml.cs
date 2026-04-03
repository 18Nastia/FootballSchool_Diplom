using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FootballSchool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        [BindProperty(SupportsGet = true)]
        public string? SearchSubscriptions { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchPayments { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "subs";

        [BindProperty(SupportsGet = true)]
        public string? SortSubscriptions { get; set; } = "student_asc";

        [BindProperty(SupportsGet = true)]
        public string? SortPayments { get; set; } = "date_desc";

        public SelectList StudentsList { get; set; } = default!;
        public SelectList ActiveSubscriptionsList { get; set; } = default!;

        public int ActiveCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<Subscription> subsQuery = _context.Subscriptions
                .Include(s => s.Student)
                .Include(s => s.Payments);

            IQueryable<Student> studentsQuery = _context.Students;

            IQueryable<Payment> paymentsQuery = _context.Payments
                .Include(p => p.Subscription)
                .ThenInclude(s => s.Student);

            if (User.IsInRole("Parent"))
            {
                var userIdStr = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdStr, out int uid))
                {
                    subsQuery = subsQuery.Where(s => s.Student.UserId == uid);
                    studentsQuery = studentsQuery.Where(s => s.UserId == uid);
                    paymentsQuery = paymentsQuery.Where(p => p.Subscription.Student.UserId == uid);
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchSubscriptions))
            {
                var subSearch = SearchSubscriptions.Trim().ToLower();

                subsQuery = subsQuery.Where(s =>
                    ((s.TypeSubscription ?? "").ToLower().Contains(subSearch)) ||
                    ((s.TermsSubscription ?? "").ToLower().Contains(subSearch)) ||
                    (((s.Student.SurnameStudent ?? "") + " " + (s.Student.NameStudent ?? "")).ToLower().Contains(subSearch)) ||
                    (((s.Student.NameStudent ?? "") + " " + (s.Student.SurnameStudent ?? "")).ToLower().Contains(subSearch))
                );
            }

            if (!string.IsNullOrWhiteSpace(SearchPayments))
            {
                var paySearch = SearchPayments.Trim().ToLower();

                paymentsQuery = paymentsQuery.Where(p =>
                    ((p.MethodPayment ?? "").ToLower().Contains(paySearch)) ||
                    ((p.StatusPayment ?? "").ToLower().Contains(paySearch)) ||
                    ((p.Subscription.TypeSubscription ?? "").ToLower().Contains(paySearch)) ||
                    (((p.Subscription.Student.SurnameStudent ?? "") + " " + (p.Subscription.Student.NameStudent ?? "")).ToLower().Contains(paySearch)) ||
                    (((p.Subscription.Student.NameStudent ?? "") + " " + (p.Subscription.Student.SurnameStudent ?? "")).ToLower().Contains(paySearch))
                );
            }

            var subs = await subsQuery.ToListAsync();

            var students = await studentsQuery
                .Select(s => new
                {
                    s.StudentId,
                    FullName = $"{s.SurnameStudent} {s.NameStudent}"
                })
                .ToListAsync();

            StudentsList = new SelectList(students, "StudentId", "FullName");

            foreach (var s in subs)
            {
                string status = (s.DaysSubscription ?? 0) > 0 ? "Активен" : "Истёк";
                string statusClass = (s.DaysSubscription ?? 0) > 0 ? "badge-active" : "badge-expired";

                if ((s.DaysSubscription ?? 0) > 0)
                {
                    ActiveCount++;
                    TotalRevenue += s.CostSubscription ?? 0;
                }

                decimal cost = s.CostSubscription ?? 0;

                decimal totalPaid = s.Payments?
                    .Where(p => (p.StatusPayment ?? "").Trim() == "Оплачен")
                    .Sum(p => p.AmountPayment) ?? 0;

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
                else if (s.Payments != null && s.Payments.Any(p => (p.StatusPayment ?? "").Trim() == "В обработке"))
                {
                    payStatus = "В обработке";
                    payClass = "warning";
                }
                else if (s.Payments != null && s.Payments.Any(p => (p.StatusPayment ?? "").Trim() == "Ошибка"))
                {
                    payStatus = "Ошибка оплаты";
                    payClass = "error";
                }

                SubscriptionsList.Add(new SubDto
                {
                    SubscriptionId = s.SubscriptionId,
                    StudentName = s.Student != null
                        ? $"{s.Student.SurnameStudent} {s.Student.NameStudent}"
                        : "Неизвестно",
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

            SubscriptionsList = (SortSubscriptions ?? "student_asc") switch
            {
                "student_desc" => SubscriptionsList.OrderByDescending(x => x.StudentName).ToList(),
                "type_asc" => SubscriptionsList.OrderBy(x => x.Type).ToList(),
                "type_desc" => SubscriptionsList.OrderByDescending(x => x.Type).ToList(),
                "cost_asc" => SubscriptionsList.OrderBy(x => x.Cost).ToList(),
                "cost_desc" => SubscriptionsList.OrderByDescending(x => x.Cost).ToList(),
                "days_asc" => SubscriptionsList.OrderBy(x => x.DaysCount).ToList(),
                "days_desc" => SubscriptionsList.OrderByDescending(x => x.DaysCount).ToList(),
                "status_asc" => SubscriptionsList.OrderBy(x => x.Status).ToList(),
                "status_desc" => SubscriptionsList.OrderByDescending(x => x.Status).ToList(),
                _ => SubscriptionsList.OrderBy(x => x.StudentName).ToList()
            };

            var payments = await paymentsQuery.ToListAsync();

            var activeSubs = await _context.Subscriptions
                .Include(s => s.Student)
                .Where(s =>
                    !User.IsInRole("Parent") ||
                    (User.FindFirst("UserId") != null &&
                     s.Student.UserId == int.Parse(User.FindFirst("UserId")!.Value)))
                .Select(s => new
                {
                    s.SubscriptionId,
                    DisplayText = $"{(s.Student != null ? s.Student.SurnameStudent : "")} - {s.TypeSubscription} ({s.CostSubscription} ₽)"
                })
                .ToListAsync();

            ActiveSubscriptionsList = new SelectList(activeSubs, "SubscriptionId", "DisplayText");

            foreach (var p in payments)
            {
                PaymentsList.Add(new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SubscriptionId = p.SubscriptionId,
                    StudentName = p.Subscription?.Student != null
                        ? $"{p.Subscription.Student.SurnameStudent} {p.Subscription.Student.NameStudent}"
                        : "Неизвестно",
                    SubscriptionInfo = p.Subscription?.TypeSubscription ?? "Неизвестный абонемент",
                    Amount = p.AmountPayment,
                    Date = new DateTime(p.DatePayment.Year, p.DatePayment.Month, p.DatePayment.Day),
                    Method = p.MethodPayment,
                    Status = (p.StatusPayment ?? "В обработке").Trim()
                });
            }

            PaymentsList = (SortPayments ?? "date_desc") switch
            {
                "date_asc" => PaymentsList.OrderBy(x => x.Date).ToList(),
                "student_asc" => PaymentsList.OrderBy(x => x.StudentName).ToList(),
                "student_desc" => PaymentsList.OrderByDescending(x => x.StudentName).ToList(),
                "amount_asc" => PaymentsList.OrderBy(x => x.Amount).ToList(),
                "amount_desc" => PaymentsList.OrderByDescending(x => x.Amount).ToList(),
                "method_asc" => PaymentsList.OrderBy(x => x.Method).ToList(),
                "method_desc" => PaymentsList.OrderByDescending(x => x.Method).ToList(),
                "status_asc" => PaymentsList.OrderBy(x => x.Status).ToList(),
                "status_desc" => PaymentsList.OrderByDescending(x => x.Status).ToList(),
                _ => PaymentsList.OrderByDescending(x => x.Date).ToList()
            };
        }

        public async Task<IActionResult> OnPostSaveSubAsync()
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new
            {
                SearchSubscriptions,
                SearchPayments,
                SortSubscriptions,
                SortPayments,
                ActiveTab = "subs"
            });
        }

        public async Task<IActionResult> OnPostDeleteSubAsync(int id)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            var sub = await _context.Subscriptions
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.SubscriptionId == id);

            if (sub != null)
            {
                if (sub.Payments.Any())
                    _context.Payments.RemoveRange(sub.Payments);

                _context.Subscriptions.Remove(sub);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Абонемент аннулирован.";
            }

            return RedirectToPage(new
            {
                SearchSubscriptions,
                SearchPayments,
                SortSubscriptions,
                SortPayments,
                ActiveTab = "subs"
            });
        }

        public async Task<IActionResult> OnPostSavePaymentAsync()
        {
            if (!User.IsInRole("Admin") && ModalPayment.PaymentId != 0)
                return Forbid();

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
                    TempData["SuccessMessage"] = User.IsInRole("Parent")
                        ? "Заявка на оплату сформирована."
                        : "Платеж зарегистрирован!";
                }
                else
                {
                    var existing = await _context.Payments.FindAsync(ModalPayment.PaymentId);
                    if (existing != null)
                    {
                        existing.StatusPayment = ModalPayment.StatusPayment;
                        existing.MethodPayment = ModalPayment.MethodPayment;
                        existing.AmountPayment = ModalPayment.AmountPayment;

                        if (ModalPayment.DatePayment != default)
                            existing.DatePayment = ModalPayment.DatePayment;

                        _context.Payments.Update(existing);
                        TempData["SuccessMessage"] = "Статус платежа обновлен!";
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка сохранения: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToPage(new
            {
                SearchSubscriptions,
                SearchPayments,
                SortSubscriptions,
                SortPayments,
                ActiveTab = "payments"
            });
        }

        public async Task<IActionResult> OnPostDeletePaymentAsync(int id)
        {
            if (!User.IsInRole("Admin"))
                return Forbid();

            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Запись о платеже удалена.";
            }

            return RedirectToPage(new
            {
                SearchSubscriptions,
                SearchPayments,
                SortSubscriptions,
                SortPayments,
                ActiveTab = "payments"
            });
        }
    }
}