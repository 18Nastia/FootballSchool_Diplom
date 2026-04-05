using FootballSchool.Controllers;
using FootballSchool.Models;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Controllers
{
    public class AccountControllerTests
    {
        private AccountController CreateController(FootballSchoolContext dbContext, bool isAuthenticated = true, string userId = "1")
        {
            var mockDataProtection = new Mock<IDataProtectionProvider>();
            var mockProtector = new Mock<IDataProtector>();
            mockDataProtection.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);
            mockProtector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(new byte[] { 1, 2, 3 });

            var mockConfig = new Mock<IConfiguration>();

            var controller = new AccountController(dbContext, mockDataProtection.Object, mockConfig.Object);

            // Настройка User Claims для тестирования авторизации
            var claims = new List<Claim>();
            if (isAuthenticated)
            {
                claims.Add(new Claim("UserId", userId));
            }

            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        [Fact]
        public async Task ChangePassword_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var controller = CreateController(context, isAuthenticated: false);
            var request = new AccountController.ChangePasswordRequest { OldPassword = "123", NewPassword = "321" };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be("Вы не авторизованы.");
        }

        [Fact]
        public async Task ChangePassword_WrongOldPassword_ReturnsBadRequest()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Users.Add(new User { UserId = 1, Login = "test", Password = "OldPassword", Role = "Admin" });
            await context.SaveChangesAsync();

            var controller = CreateController(context, isAuthenticated: true, userId: "1");
            var request = new AccountController.ChangePasswordRequest { OldPassword = "WrongPassword", NewPassword = "NewPassword123" };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Неверный текущий пароль.");
        }

        [Fact]
        public async Task ChangePassword_ValidRequest_UpdatesPasswordAndReturnsOk()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var user = new User { UserId = 1, Login = "test", Password = "CorrectOldPassword", Role = "Admin" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = CreateController(context, isAuthenticated: true, userId: "1");
            var request = new AccountController.ChangePasswordRequest { OldPassword = "CorrectOldPassword", NewPassword = "NewSecurePassword" };

            // Act
            var result = await controller.ChangePassword(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Убеждаемся, что пароль в БД действительно изменился
            var updatedUser = await context.Users.FindAsync(1);
            updatedUser.Password.Should().Be("NewSecurePassword");
        }

        [Fact]
        public async Task ForgotPassword_EmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var controller = CreateController(context);
            var request = new AccountController.ForgotPasswordRequest { Email = "" };

            // Act
            var result = await controller.ForgotPassword(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Email обязателен");
        }

        [Fact]
        public async Task ForgotPassword_UserNotFound_ReturnsOkToPreventEnumeration()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var controller = CreateController(context);
            var request = new AccountController.ForgotPasswordRequest { Email = "notfound@example.com" };

            // Act
            var result = await controller.ForgotPassword(request);

            // Assert
            // Мы возвращаем OK даже если email не найден, чтобы защититься от подбора email-ов злоумышленниками
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "Если email существует в системе, письмо отправлено." });
        }
    }
}