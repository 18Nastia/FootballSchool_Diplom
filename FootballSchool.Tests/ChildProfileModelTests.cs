using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class ChildProfileModelTests
    {
        private ChildProfileModel CreateModel(FootballSchoolContext context, string role = "Admin", string userId = null)
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath\\wwwroot");

            var model = new ChildProfileModel(context, mockEnvironment.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            if (userId != null)
            {
                claims.Add(new Claim("UserId", userId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };

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
        public async Task OnGetAsync_ParentWithoutId_LoadsOwnChildProfile()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            context.Users.Add(new User { UserId = 5, Login = "p", Password = "p", Role = "Parent" });
            context.Students.Add(new Student { StudentId = 100, UserId = 5, NameStudent = "Свой", SurnameStudent = "Ребенок", BirthStudent = new DateOnly(2010, 1, 1), GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Parent", userId: "5");

            // Act
            var result = await model.OnGetAsync(id: null);

            // Assert
            result.Should().BeOfType<PageResult>();
            model.StudentProfile.Should().NotBeNull();
            model.StudentProfile.StudentId.Should().Be(100);
            model.StudentProfile.FullName.Should().Contain("Свой");
            model.StudentProfile.Initials.Should().Be("СР");
        }

        [Fact]
        public async Task OnGetAsync_WithValidId_PopulatesProfileDtoCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var birthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10)); // Возраст 10 лет

            context.Students.Add(new Student
            {
                StudentId = 1,
                NameStudent = "Алексей",
                SurnameStudent = "Смирнов",
                BirthStudent = birthDate,
                GenderStudent = "М",
                LevelStudent = "Профи",
                ParentNumber = "1",
                SurnameParent = "Смирнов",
                NameParent = "Иван",
                CityStudent = "Москва",
                StreetStudent = "Ленина",
                HouseStudent = "1"
            });

            // Добавляем посещение, чтобы сработала ачивка "Первый шаг"
            context.Attendances.Add(new Attendance { AttendanceId = 1, StudentId = 1, StatusAttendance = "Был" });
            await context.SaveChangesAsync();

            var model = CreateModel(context, "Admin");

            // Act
            var result = await model.OnGetAsync(1);

            // Assert
            result.Should().BeOfType<PageResult>();
            model.StudentProfile.Should().NotBeNull();
            model.StudentProfile.Age.Should().Be(10);
            model.StudentProfile.Level.Should().Be("Профи");

            // Проверка генерации автоматических достижений (Геймификация)
            model.StudentProfile.Achievements.Should().Contain(a => a.Title == "Первый шаг");
            model.StudentProfile.Achievements.Should().Contain(a => a.Title == "Элитный статус"); // Из-за LevelStudent = "Профи"
        }

        [Fact]
        public async Task OnPostEditAsync_UpdatesStudentData()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Students.Add(new Student { StudentId = 1, NameStudent = "СтароеИмя", SurnameStudent = "С", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");
            model.EditStudent = new Student
            {
                StudentId = 1,
                NameStudent = "НовоеИмя",
                SurnameStudent = "С",
                GenderStudent = "М",
                LevelStudent = "Продвинутый",
                ParentNumber = "2",
                SurnameParent = "П",
                NameParent = "П",
                CityStudent = "М",
                StreetStudent = "С",
                HouseStudent = "1"
            };

            // Act
            var result = await model.OnPostEditAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var updatedStudent = await context.Students.FindAsync(1);
            updatedStudent.NameStudent.Should().Be("НовоеИмя");
            updatedStudent.LevelStudent.Should().Be("Продвинутый");
        }

        [Fact]
        public async Task OnPostAddAchievementAsync_AdminRole_AddsProgressRecord()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context, "Admin");
            model.NewAchievement = new ChildProfileModel.NewAchievementModel
            {
                Title = "Лучший бомбардир",
                Description = "Забил больше всего голов в сезоне",
                Date = DateTime.Today
            };

            // Act
            var result = await model.OnPostAddAchievementAsync(1, null);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var progress = await context.Progresses.FirstOrDefaultAsync(p => p.StudentId == 1);
            progress.Should().NotBeNull();
            progress.TestsProgress.Should().Be("ACHIEVEMENT|Лучший бомбардир");
            progress.PlanProgress.Should().Be("Забил больше всего голов в сезоне");
        }

        [Fact]
        public async Task OnPostDeleteAsync_AdminRole_DeletesStudentAndRelatedData()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Создаем связанного пользователя
            context.Users.Add(new User { UserId = 2, Login = "test", Password = "123", Role = "Parent" });

            // Создаем ученика
            context.Students.Add(new Student { StudentId = 10, UserId = 2, NameStudent = "Удаляемый", SurnameStudent = "С", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            // Создаем связанные данные, которые должны удалиться каскадно
            context.Subscriptions.Add(new Subscription { SubscriptionId = 5, StudentId = 10, TypeSubscription = "Базовый" });
            context.Progresses.Add(new Progress { ProgressId = 3, StudentId = 10, DateProgress = DateOnly.FromDateTime(DateTime.Today) });

            await context.SaveChangesAsync();

            var model = CreateModel(context, "Admin");

            // Act
            var result = await model.OnPostDeleteAsync(10);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var student = await context.Students.FindAsync(10);
            student.Should().BeNull(); // Ученик удален

            var user = await context.Users.FindAsync(2);
            user.Should().BeNull(); // Связанный аккаунт родителя тоже должен быть удален

            var sub = await context.Subscriptions.FindAsync(5);
            sub.Should().BeNull(); // Абонемент удален

            var progress = await context.Progresses.FindAsync(3);
            progress.Should().BeNull(); // Прогресс удален
        }

        [Fact]
        public async Task OnPostDeleteAsync_NotAdmin_ReturnsAccessDenied()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context, "Coach"); // Тестируем доступ от тренера

            // Act
            var result = await model.OnPostDeleteAsync(1);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/AccessDenied");
        }
    }
}