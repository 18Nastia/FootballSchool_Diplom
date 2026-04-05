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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class AttendanceModelTests
    {
        private AttendanceModel CreateModel(FootballSchoolContext context, string role = "Admin", string coachId = null)
        {
            var model = new AttendanceModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            if (coachId != null)
            {
                claims.Add(new Claim("CoachId", coachId));
            }

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
        public async Task OnGetAsync_DailyView_LoadsAvailableTrainings()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            // ИСПОЛЬЗУЕМ ФИКСИРОВАННУЮ ДАТУ
            var fixedDate = new DateTime(2026, 5, 15);
            var dateOnly = DateOnly.FromDateTime(fixedDate);

            // Сидируем данные: Филиал, Группа, Ученик, Площадка
            context.Branches.Add(new Branch { BranchId = 1, NameBranch = "Центр", CityBranch = "Мск", StreetBranch = "Ленина", HouseBranch = "1", PhoneBranch = "123" });
            context.Teams.Add(new Team { TeamId = 1, CategoryTeam = "Юниоры", StatusTeam = "Активна", BranchId = 1 });
            context.Students.Add(new Student { StudentId = 1, TeamId = 1, NameStudent = "Иван", SurnameStudent = "Иванов", GenderStudent = "М", LevelStudent = "Новичок", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            context.Facilities.Add(new Facility { FacilityId = 1, BranchId = 1, NameFacility = "Поле 1", TypeFacility = "Поле", CapacityFacility = 20, StatusFacility = "Активен" });
            context.Coaches.Add(new Coach { CoachId = 1, SurnameCoach = "Тренер", NameCoach = "Тренеров", QualificationCoach = "КМС", SpecialtyCoach = "Футбол" });

            context.Training.Add(new Training { TrainingId = 1, TeamId = 1, FacilityId = 1, CoachId = 1, DateTraining = dateOnly, TimeTraining = new TimeOnly(15, 0) });
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");
            model.FilterTeamId = 1;
            model.FilterDate = fixedDate;
            model.ViewMode = "Daily";

            // Act
            await model.OnGetAsync();

            // Assert
            model.AvailableTrainings.Should().HaveCount(1);
            model.AvailableTrainings.First().TrainingId.Should().Be(1);
            model.AttendanceItems.Should().HaveCount(1); // Должен подгрузиться 1 ученик для отметки
            model.AttendanceItems.First().StudentName.Should().Be("Иванов Иван");
        }

        [Fact]
        public async Task OnPostSaveAsync_ValidData_SavesAttendance()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            model.SelectedTrainingId = 10;
            model.AttendanceItems = new List<AttendanceModel.AttendanceItem>
            {
                new AttendanceModel.AttendanceItem { StudentId = 1, Status = "Был", Comment = "Молодец" },
                new AttendanceModel.AttendanceItem { StudentId = 2, Status = "Не был", Comment = "Болеет" }
            };

            // Act
            var result = await model.OnPostSaveAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var attendances = await context.Attendances.ToListAsync();
            attendances.Should().HaveCount(2);
            attendances.Should().ContainSingle(a => a.StudentId == 1 && a.StatusAttendance == "Был" && a.NoteAttendance == "Молодец");
            attendances.Should().ContainSingle(a => a.StudentId == 2 && a.StatusAttendance == "Не был");
        }
    }
}