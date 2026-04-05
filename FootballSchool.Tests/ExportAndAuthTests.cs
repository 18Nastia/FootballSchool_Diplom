using FootballSchool.Models;
using FootballSchool.Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class ExportAndAuthTests
    {
        // 1. ТЕСТИРОВАНИЕ ВЫХОДА ИЗ СИСТЕМЫ (LOGOUT)
        [Fact]
        public async Task LoginModel_OnPostLogoutAsync_SignsOutAndRedirectsToLogin()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var authServiceMock = new Mock<IAuthenticationService>();

            // Настраиваем мок так, чтобы он успешно имитировал процесс SignOutAsync
            authServiceMock
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProvider = new ServiceCollection()
                .AddSingleton(authServiceMock.Object)
                .BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var actionContext = new ActionContext(
                httpContext,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            );

            var model = new LoginModel(context)
            {
                PageContext = new PageContext(actionContext)
            };

            // Act
            var result = await model.OnPostLogoutAsync();

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/Login");

            // Проверяем, что метод SignOutAsync был действительно вызван ровно 1 раз
            authServiceMock.Verify(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        // 2. ТЕСТИРОВАНИЕ ЭКСПОРТА ПОСЕЩАЕМОСТИ В EXCEL (ClosedXML)
        [Fact]
        public async Task AttendanceModel_OnGetExportReportAsync_ReturnsExcelFile()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Подготавливаем минимальные данные для отчета
            context.Teams.Add(new Team { TeamId = 1, CategoryTeam = "Тестовая группа", StatusTeam = "Активна", BranchId = 1 });
            context.Students.Add(new Student { StudentId = 1, TeamId = 1, NameStudent = "Иван", SurnameStudent = "Иванов", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            context.Training.Add(new Training { TrainingId = 1, TeamId = 1, CoachId = 1, FacilityId = 1, DateTraining = DateOnly.FromDateTime(DateTime.Today), TimeTraining = new TimeOnly(15, 0) });
            context.Attendances.Add(new Attendance { AttendanceId = 1, TrainingId = 1, StudentId = 1, StatusAttendance = "Был" });

            await context.SaveChangesAsync();

            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();

            var model = new AttendanceModel(context)
            {
                PageContext = new PageContext(new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())),
                TempData = new TempDataDictionary(httpContext, tempDataProvider.Object)
            };

            // Act
            var result = await model.OnGetExportReportAsync(teamId: 1, period: "month");

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().Contain("Журнал_посещаемости_Тестовая группа");
            fileResult.FileContents.Should().NotBeEmpty(); // Файл не должен быть пустым
        }

        [Fact]
        public async Task AttendanceModel_OnGetExportReportAsync_InvalidTeam_ReturnsNotFound()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = new AttendanceModel(context);

            // Act
            var result = await model.OnGetExportReportAsync(teamId: 999, period: "month"); // Несуществующая группа

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        // 3. ТЕСТИРОВАНИЕ ЭКСПОРТА РЕЗУЛЬТАТОВ ТРЕНИРОВОК В EXCEL (ClosedXML)
        [Fact]
        public async Task TrainingResultsModel_OnGetExportDetailedAsync_ReturnsExcelFile()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Группа, Ученик и Прогресс (Достижение/Тест)
            context.Teams.Add(new Team { TeamId = 1, CategoryTeam = "Основа", StatusTeam = "Активна", BranchId = 1 });
            context.Students.Add(new Student { StudentId = 1, TeamId = 1, NameStudent = "Алексей", SurnameStudent = "Смирнов", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            context.Progresses.Add(new Progress { ProgressId = 1, StudentId = 1, DateProgress = DateOnly.FromDateTime(DateTime.Today), TestsProgress = "Скорость|9.5 сек", PlanProgress = "Отлично" });

            await context.SaveChangesAsync();

            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();

            var model = new TrainingResultsModel(context)
            {
                PageContext = new PageContext(new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())),
                TempData = new TempDataDictionary(httpContext, tempDataProvider.Object)
            };

            // Act: Экспортируем историю по конкретному ученику
            var result = await model.OnGetExportDetailedAsync(teamId: null, studentId: 1);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().Contain("История_результатов_Смирнов_Алексей");
            fileResult.FileContents.Should().NotBeEmpty(); // Проверка, что байтовый массив Excel-файла сгенерирован
        }

        [Fact]
        public async Task TrainingResultsModel_OnGetExportAsync_ReturnsSummaryExcelFile()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Для сводки требуется просто чтобы OnGetAsync отработал корректно
            context.Students.Add(new Student { StudentId = 1, NameStudent = "Петр", SurnameStudent = "Петров", BirthStudent = new DateOnly(2010, 1, 1), GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            await context.SaveChangesAsync();

            // Создаем мок User с ролью Admin, так как OnGetAsync обращается к Claims
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            var tempDataProvider = new Mock<ITempDataProvider>();

            var model = new TrainingResultsModel(context)
            {
                PageContext = new PageContext(new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor())),
                TempData = new TempDataDictionary(httpContext, tempDataProvider.Object)
            };

            // Act
            var result = await model.OnGetExportAsync();

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().Contain("Сводка_тестов");
            fileResult.FileContents.Should().NotBeEmpty();
        }
    }
}