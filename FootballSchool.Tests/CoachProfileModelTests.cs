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
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class CoachProfileModelTests
    {
        private CoachProfileModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath\\wwwroot");

            var model = new CoachProfileModel(context, mockEnvironment.Object);

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
        public async Task OnGetAsync_BuildsProfileAndAwardsCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            context.Coaches.Add(new Coach
            {
                CoachId = 1,
                NameCoach = "Юрген",
                SurnameCoach = "Клопп",
                QualificationCoach = "PRO License",
                SpecialtyCoach = "Тактика"
            });

            // Добавляем команду и 6 тренировок (для получения награды "Опытный тренер" >= 5 тренировок)
            context.Teams.Add(new Team { TeamId = 1, CategoryTeam = "Основа", BranchId = 1, StatusTeam = "Актив" });

            for (int i = 1; i <= 6; i++)
            {
                context.Training.Add(new Training
                {
                    TrainingId = i,
                    CoachId = 1,
                    TeamId = 1,
                    FacilityId = 1,
                    DateTraining = DateOnly.FromDateTime(DateTime.Today.AddDays(i)),
                    TimeTraining = new TimeOnly(18, 0)
                });
            }

            await context.SaveChangesAsync();
            var model = CreateModel(context);

            // Act
            var result = await model.OnGetAsync(1);

            // Assert
            result.Should().BeOfType<PageResult>();
            model.Profile.Should().NotBeNull();
            model.Profile.FullName.Should().Be("Клопп Юрген");

            // Проверка групп
            model.Profile.Groups.Should().HaveCount(1);
            model.Profile.Groups[0].CategoryName.Should().Be("Основа");

            // Проверка наград (Квалификация, Специализация + 1 достижение за количество тренировок)
            model.Profile.Awards.Should().HaveCount(3);
            model.Profile.Awards.Should().Contain(a => a.Title == "Опытный тренер");
            model.Profile.Awards.Should().Contain(a => a.Description == "PRO License");
        }

        [Fact]
        public async Task OnPostEditAsync_UpdatesCoachData()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Coaches.Add(new Coach { CoachId = 1, NameCoach = "Тренер", SurnameCoach = "Старый", QualificationCoach = "КМС", SpecialtyCoach = "Бег" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.EditCoach = new Coach
            {
                CoachId = 1,
                NameCoach = "Тренер",
                SurnameCoach = "Новый", // Изменили фамилию
                QualificationCoach = "МС", // Изменили квалификацию
                SpecialtyCoach = "Бег",
                SalaryCoach = 50000
            };

            // Act
            var result = await model.OnPostEditAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var updatedCoach = await context.Coaches.FindAsync(1);
            updatedCoach.SurnameCoach.Should().Be("Новый");
            updatedCoach.QualificationCoach.Should().Be("МС");
            updatedCoach.SalaryCoach.Should().Be(50000);
        }
    }
}