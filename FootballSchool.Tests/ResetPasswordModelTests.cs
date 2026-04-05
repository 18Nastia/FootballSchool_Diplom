using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class ResetPasswordModelTests
    {
        private IDataProtectionProvider _dataProtectionProvider;

        public ResetPasswordModelTests()
        {
            // Используем реальный провайдер защиты данных для тестов, 
            // так как мокать методы-расширения Protect/Unprotect слишком сложно и ненадежно.
            var services = new ServiceCollection();
            services.AddDataProtection();
            var serviceProvider = services.BuildServiceProvider();
            _dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        }

        private ResetPasswordModel CreateModel(FootballSchoolContext context)
        {
            return new ResetPasswordModel(context, _dataProtectionProvider);
        }

        [Fact]
        public void OnGet_EmptyToken_RedirectsToLogin()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.Token = null;

            // Act
            var result = model.OnGet();

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/Login");
        }

        [Fact]
        public async Task OnPostAsync_PasswordsDoNotMatch_ReturnsError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.Token = "SomeToken";
            model.NewPassword = "Password123";
            model.ConfirmPassword = "Password321"; // Не совпадают

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().Be("Пароли не совпадают.");
            model.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task OnPostAsync_ExpiredToken_ReturnsErrorMessage()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            // Создаем токен, который истек 1 час назад
            var protector = _dataProtectionProvider.CreateProtector("PasswordReset");
            long pastTicks = DateTime.UtcNow.AddHours(-1).Ticks;
            string rawToken = $"testuser|{pastTicks}";
            model.Token = protector.Protect(rawToken);

            model.NewPassword = "NewPassword123";
            model.ConfirmPassword = "NewPassword123";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().Contain("Срок действия ссылки истек");
            model.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task OnPostAsync_ValidToken_UpdatesPasswordAndSetsSuccess()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Добавляем пользователя
            context.Users.Add(new User { UserId = 1, Login = "targetuser", Password = "OldPassword", Role = "Parent" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);

            // Создаем валидный токен, действующий еще 1 час
            var protector = _dataProtectionProvider.CreateProtector("PasswordReset");
            long futureTicks = DateTime.UtcNow.AddHours(1).Ticks;
            string rawToken = $"targetuser|{futureTicks}";
            model.Token = protector.Protect(rawToken);

            model.NewPassword = "BrandNewSecurePassword";
            model.ConfirmPassword = "BrandNewSecurePassword";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().BeEmpty();
            model.IsSuccess.Should().BeTrue();

            // Проверяем базу данных
            var updatedUser = await context.Users.FindAsync(1);
            updatedUser.Password.Should().Be("BrandNewSecurePassword"); // Пароль успешно обновлен
        }
    }
}