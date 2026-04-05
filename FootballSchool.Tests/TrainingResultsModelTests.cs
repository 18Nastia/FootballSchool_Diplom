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
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class TrainingResultsModelTests
    {
        private TrainingResultsModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var model = new TrainingResultsModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
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
        public async Task OnGetAsync_BuildsStudentsHistoryJsonCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            context.Students.Add(new Student { StudentId = 1, NameStudent = "Алексей", SurnameStudent = "А", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            // Тест, который должен распарситься корректно
            context.Progresses.Add(new Progress
            {
                ProgressId = 1,
                StudentId = 1,
                DateProgress = new DateOnly(2026, 1, 1),
                TestsProgress = "Скорость|9.5 сек",
                PlanProgress = "Отлично"
            });

            await context.SaveChangesAsync();
            var model = CreateModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            model.StudentsList.Should().HaveCount(1);
            model.StudentsList[0].LatestTestName.Should().Be("Скорость");
            model.StudentsList[0].LatestTestResult.Should().Be("9.5 сек");

            // ИСПРАВЛЕНИЕ:
            // JsonSerializer по умолчанию экранирует кириллицу (Unicode escape sequences \u0421...).
            // Поэтому парсим JSON с помощью JsonDocument и жестко проверяем свойства.
            var jsonDoc = JsonDocument.Parse(model.StudentsHistoryJson);
            var firstRecord = jsonDoc.RootElement.GetProperty("1")[0];

            firstRecord.GetProperty("type").GetString().Should().Be("Скорость");
            firstRecord.GetProperty("value").GetDouble().Should().Be(9.5);
        }

        [Fact]
        public async Task OnPostSaveResultAsync_NoStudentId_ReturnsError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.SelectedStudentId = 0;

            // Act
            var result = await model.OnPostSaveResultAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            model.TempData["ErrorMessage"].ToString().Should().Contain("не выбран ученик");
        }

        [Fact]
        public async Task OnPostSaveResultAsync_ValidData_SavesProgress()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Students.Add(new Student { StudentId = 1, NameStudent = "Студент", SurnameStudent = "Студентов", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.SelectedStudentId = 1;
            model.EditingResultId = 0;
            model.TestType = "Сила удара";
            model.TestResult = "120,5"; // С запятой, должен сконвертироваться корректно
            model.ResultUnit = "км/ч";
            model.TestDate = new DateTime(2026, 5, 20);

            // Act
            var result = await model.OnPostSaveResultAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var savedProgress = await context.Progresses.FirstOrDefaultAsync();
            savedProgress.Should().NotBeNull();
            savedProgress.TestsProgress.Should().Be("Сила удара|120.5 км/ч"); // Запятая заменяется на точку
        }
    }
}