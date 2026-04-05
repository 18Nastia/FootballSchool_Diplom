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
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class ScheduleModelTests
    {
        private ScheduleModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var model = new ScheduleModel(context);

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
        public async Task OnGetAsync_CalculatesWeekBoundsCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            // Устанавливаем среду, 20 мая 2026 года
            model.CurrentDate = new DateTime(2026, 5, 20);

            // Act
            await model.OnGetAsync();

            // Assert
            // Понедельник той же недели - 18 мая 2026
            model.StartOfWeek.Should().Be(new DateTime(2026, 5, 18));
            // Воскресенье той же недели - 24 мая 2026
            model.EndOfWeek.Should().Be(new DateTime(2026, 5, 24));
        }

        [Fact]
        public async Task OnPostSaveAsync_AdminRole_AddsTraining()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Группа и площадка должны быть в одном филиале (BranchId = 1)
            context.Branches.Add(new Branch { BranchId = 1, NameBranch = "Branch", CityBranch = "A", StreetBranch = "B", HouseBranch = "C", PhoneBranch = "D" });
            context.Teams.Add(new Team { TeamId = 1, BranchId = 1, CategoryTeam = "Team A", StatusTeam = "Active" });
            context.Facilities.Add(new Facility { FacilityId = 1, BranchId = 1, NameFacility = "Hall", TypeFacility = "A", CapacityFacility = 10, StatusFacility = "Active" });
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");
            model.ModalTraining = new Training
            {
                TrainingId = 0,
                TeamId = 1,
                FacilityId = 1,
                CoachId = 1,
                DateTraining = new DateOnly(2026, 5, 20),
                TimeTraining = new TimeOnly(18, 0),
                PlanTraining = "Пасы"
            };

            // Act
            var result = await model.OnPostSaveAsync(isBulkAdd: false);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var savedTraining = await context.Training.FirstOrDefaultAsync();
            savedTraining.Should().NotBeNull();
            savedTraining.PlanTraining.Should().Be("Пасы");
        }

        [Fact]
        public async Task OnPostSaveAsync_DifferentBranches_ReturnsError()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Группа и площадка в РАЗНЫХ филиалах
            context.Branches.Add(new Branch { BranchId = 1, NameBranch = "B1", CityBranch = "A", StreetBranch = "B", HouseBranch = "C", PhoneBranch = "D" });
            context.Branches.Add(new Branch { BranchId = 2, NameBranch = "B2", CityBranch = "A", StreetBranch = "B", HouseBranch = "C", PhoneBranch = "D" });

            context.Teams.Add(new Team { TeamId = 1, BranchId = 1, CategoryTeam = "Team A", StatusTeam = "Active" });
            context.Facilities.Add(new Facility { FacilityId = 1, BranchId = 2, NameFacility = "Hall", TypeFacility = "A", CapacityFacility = 10, StatusFacility = "Active" });
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");
            model.ModalTraining = new Training
            {
                TrainingId = 0,
                TeamId = 1,     // Привязан к Branch 1
                FacilityId = 1  // Привязан к Branch 2
            };

            // Act
            var result = await model.OnPostSaveAsync(isBulkAdd: false);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            model.TempData["ErrorMessage"].ToString().Should().Contain("относиться к одному филиалу");

            var savedTraining = await context.Training.FirstOrDefaultAsync();
            savedTraining.Should().BeNull(); // Не сохранилось
        }
    }
}