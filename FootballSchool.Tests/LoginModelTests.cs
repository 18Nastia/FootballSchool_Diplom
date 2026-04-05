using FluentAssertions;
using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class LoginModelTests
    {
        private LoginModel CreateModel(FootballSchoolContext context)
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProvider = new ServiceCollection()
                .AddSingleton(authServiceMock.Object)
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

            // Исправлено: Передаем RouteData и ActionDescriptor для предотвращения ArgumentNullException
            var actionContext = new ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );

            var pageContext = new PageContext(actionContext);

            return new LoginModel(context)
            {
                PageContext = pageContext
            };
        }

        [Fact]
        public async Task OnGetAsync_EmptyDatabase_CreatesDefaultAdmin()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Login == "admin");
            adminUser.Should().NotBeNull();
            adminUser.Password.Should().Be("admin");
            adminUser.Role.Should().Be("Admin");
            model.InfoMessage.Should().Contain("Создан аккаунт администратора");
        }

        [Fact]
        public async Task OnPostAsync_EmptyCredentials_ReturnsPageWithError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.Login = "";
            model.Password = "";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().Be("Пожалуйста, введите логин и пароль.");
        }

        [Fact]
        public async Task OnPostAsync_InvalidCredentials_ReturnsPageWithError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Users.Add(new User { Login = "user", Password = "correct", Role = "Parent" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.Login = "user";
            model.Password = "wrong";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().Be("Неверный логин или пароль.");
        }

        [Fact]
        public async Task OnPostAsync_ValidAdmin_RedirectsToAdminIndex()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Users.Add(new User { Login = "admin", Password = "123", Role = "Admin" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.Login = "admin";
            model.Password = "123";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/Main_Pages/Index_Admin");
        }

        [Fact]
        public async Task OnPostAsync_ValidCoachWithoutProfile_ReturnsError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            // Пользователь создан, но профиля Coach в таблице Coaches нет
            context.Users.Add(new User { UserId = 1, Login = "coach", Password = "123", Role = "Coach" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.Login = "coach";
            model.Password = "123";

            // Act
            var result = await model.OnPostAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.ErrorMessage.Should().Be("Для данного аккаунта тренера не найден связанный профиль.");
        }
    }
}