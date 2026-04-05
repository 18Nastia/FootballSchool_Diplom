using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class GroupsStudentsModelTests
    {
        private GroupsStudentsModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath\\wwwroot");

            var model = new GroupsStudentsModel(context, mockEnvironment.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // ИСПРАВЛЕНИЕ: Инжектим IObjectModelValidator через RequestServices для TryValidateModel
            var objectValidatorMock = new Mock<IObjectModelValidator>();
            objectValidatorMock.Setup(o => o.Validate(
                It.IsAny<ActionContext>(),
                It.IsAny<ValidationStateDictionary>(),
                It.IsAny<string>(),
                It.IsAny<object>()));

            var services = new ServiceCollection();
            services.AddSingleton(objectValidatorMock.Object);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal,
                RequestServices = services.BuildServiceProvider()
            };

            model.PageContext = new PageContext(new ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            ));

            var tempDataProvider = new Mock<ITempDataProvider>();
            model.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

            return model;
        }

        [Fact]
        public async Task OnPostAddTeamAsync_ValidData_AddsTeam()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.NewTeam = new Team
            {
                CategoryTeam = "Новая тестовая группа",
                BranchId = 1,
                StatusTeam = "Активна"
            };

            // Act
            var result = await model.OnPostAddTeamAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var teamInDb = await context.Teams.FirstOrDefaultAsync(t => t.CategoryTeam == "Новая тестовая группа");
            teamInDb.Should().NotBeNull();
            teamInDb.StatusTeam.Should().Be("Активна");
        }

        [Fact]
        public async Task OnPostAddStudentAsync_CreatesStudentAndParentUser()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            model.NewStudent = new Student
            {
                NameStudent = "Петр",
                SurnameStudent = "Петров",
                BirthStudent = new System.DateOnly(2015, 1, 1),
                GenderStudent = "Мужской",
                LevelStudent = "Новичок",
                SurnameParent = "Петров",
                NameParent = "Алексей",
                ParentNumber = "+79991234567",
                CityStudent = "Москва",
                StreetStudent = "Тверская",
                HouseStudent = "10"
            };
            model.ParentEmail = "parent@test.com";

            // Act
            var result = await model.OnPostAddStudentAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            // Проверяем создание ученика
            var studentInDb = await context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.NameStudent == "Петр");
            studentInDb.Should().NotBeNull();
            studentInDb.ParentNumber.Should().Be("+79991234567");

            // Проверяем автоматическое создание пользователя-родителя
            studentInDb.User.Should().NotBeNull();
            studentInDb.User.Role.Should().Be("Parent");
            studentInDb.User.Email.Should().Be("parent@test.com");
            studentInDb.User.Login.Should().StartWith("parent_petrov");
        }

        [Fact]
        public async Task OnPostDeleteTeamAsync_NullifiesStudentsTeamIdAndDeletesTeam()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Teams.Add(new Team { TeamId = 10, CategoryTeam = "To Delete", BranchId = 1, StatusTeam = "Активна" });
            context.Students.Add(new Student { StudentId = 1, TeamId = 10, NameStudent = "Ученик 1", SurnameStudent = "С", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);

            // Act
            var result = await model.OnPostDeleteTeamAsync(10);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var deletedTeam = await context.Teams.FindAsync(10);
            deletedTeam.Should().BeNull(); // Группа удалена

            var student = await context.Students.FindAsync(1);
            student.TeamId.Should().BeNull(); // Ученик остался, но TeamId обнулился
        }
    }
}