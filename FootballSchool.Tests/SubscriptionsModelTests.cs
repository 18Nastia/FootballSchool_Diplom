using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class SubscriptionsModelTests
    {
        private SubscriptionsModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var model = new SubscriptionsModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

            model.PageContext = new PageContext(new ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            ));

            // Исправление NullReferenceException для TempData
            var tempDataProvider = new Mock<ITempDataProvider>();
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            return model;
        }

        [Fact]
        public async Task OnGetAsync_CalculatesFinancesCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Добавляем ученика
            context.Students.Add(new Student { StudentId = 1, NameStudent = "Студент", SurnameStudent = "Студентов", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            // Добавляем Активный абонемент (Долг: 3000 - 1000 = 2000)
            context.Subscriptions.Add(new Subscription { SubscriptionId = 1, StudentId = 1, TypeSubscription = "Базовый", DaysSubscription = 8, CostSubscription = 3000 });
            context.Payments.Add(new Payment { PaymentId = 1, SubscriptionId = 1, AmountPayment = 1000, StatusPayment = "Оплачен", MethodPayment = "Наличные" });

            // Добавляем Истекший абонемент (Полностью оплачен)
            context.Subscriptions.Add(new Subscription { SubscriptionId = 2, StudentId = 1, TypeSubscription = "Разовое", DaysSubscription = 0, CostSubscription = 500 });
            context.Payments.Add(new Payment { PaymentId = 2, SubscriptionId = 2, AmountPayment = 500, StatusPayment = "Оплачен", MethodPayment = "Карта" });

            await context.SaveChangesAsync();

            var model = CreateModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            model.ActiveCount.Should().Be(1); // Только 1 активный (DaysSubscription > 0)
            model.TotalRevenue.Should().Be(3000); // Потенциальная выручка с активных абонементов

            model.SubscriptionsList.Should().HaveCount(2);
            var activeSubDto = model.SubscriptionsList.Find(s => s.SubscriptionId == 1);
            activeSubDto.Status.Should().Be("Активен");
            activeSubDto.PaymentStatus.Should().Be("Долг 2000 ₽"); // Вычисляемое поле

            var expiredSubDto = model.SubscriptionsList.Find(s => s.SubscriptionId == 2);
            expiredSubDto.Status.Should().Be("Истёк");
            expiredSubDto.PaymentStatus.Should().Be("Оплачен");
        }

        [Fact]
        public async Task OnPostSavePaymentAsync_ParentRole_CreatesOnlinePaymentRequest()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context, role: "Parent");

            model.ModalPayment = new Payment
            {
                PaymentId = 0,
                SubscriptionId = 1,
                AmountPayment = 1500
                // Parent не должен сам задавать статус, метод проверит переопределение в контроллере
            };

            // Act
            var result = await model.OnPostSavePaymentAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var paymentInDb = await context.Payments.FirstOrDefaultAsync();
            paymentInDb.Should().NotBeNull();
            paymentInDb.AmountPayment.Should().Be(1500);

            // Должно быть принудительно переопределено внутри метода OnPostSavePaymentAsync для Parent
            paymentInDb.StatusPayment.Should().Be("В обработке");
            paymentInDb.MethodPayment.Should().Be("Онлайн-эквайринг");
        }

        [Fact]
        public async Task OnPostDeleteSubAsync_AdminRole_DeletesSubAndRelatedPayments()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Subscriptions.Add(new Subscription { SubscriptionId = 10, StudentId = 1, TypeSubscription = "Удалить" });
            context.Payments.Add(new Payment { PaymentId = 5, SubscriptionId = 10, AmountPayment = 1000, MethodPayment = "Кэш" });
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");

            // Act
            var result = await model.OnPostDeleteSubAsync(10);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var subInDb = await context.Subscriptions.FindAsync(10);
            subInDb.Should().BeNull();

            var paymentInDb = await context.Payments.FindAsync(5);
            paymentInDb.Should().BeNull(); // Платежи, связанные с абонементом, должны каскадно удаляться
        }
    }
}