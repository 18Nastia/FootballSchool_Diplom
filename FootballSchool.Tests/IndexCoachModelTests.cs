using FootballSchool.Models;
using FootballSchool.Pages.Main_Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class IndexCoachModelTests
    {
        private Index_CoachModel CreateModel(FootballSchoolContext context, string coachId = null)
        {
            var model = new Index_CoachModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Coach") };
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

            return model;
        }

        [Fact]
        public async Task OnGetAsync_NoCoachClaim_RedirectsToLogin()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context, coachId: null);

            // Act
            var result = await model.OnGetAsync();

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/Login");
        }

        [Fact]
        public async Task OnGetAsync_ValidCoach_CalculatesStatisticsCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var yesterday = today.AddDays(-1);

            // ИСПРАВЛЕНИЕ: Добавляем все необходимые зависимости, чтобы избежать отсечения через INNER JOIN
            context.Branches.Add(new Branch { BranchId = 1, NameBranch = "Центр", CityBranch = "Мск", StreetBranch = "С", HouseBranch = "1", PhoneBranch = "1" });
            context.Facilities.Add(new Facility { FacilityId = 1, BranchId = 1, NameFacility = "Зал 1", TypeFacility = "Поле", CapacityFacility = 20, StatusFacility = "Активен" });

            context.Coaches.Add(new Coach { CoachId = 1, NameCoach = "Иван", SurnameCoach = "Иванов", QualificationCoach = "КМС", SpecialtyCoach = "Футбол" });
            context.Teams.Add(new Team { TeamId = 1, CategoryTeam = "U10", StatusTeam = "Активна", BranchId = 1 });

            // 2 ученика в группе тренера
            context.Students.Add(new Student { StudentId = 1, TeamId = 1, NameStudent = "А", SurnameStudent = "Б", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });
            context.Students.Add(new Student { StudentId = 2, TeamId = 1, NameStudent = "В", SurnameStudent = "Г", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "П", NameParent = "П", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            // Тренировка сегодня (Upcoming)
            context.Training.Add(new Training { TrainingId = 1, CoachId = 1, TeamId = 1, FacilityId = 1, DateTraining = today, TimeTraining = new TimeOnly(15, 0) });

            // Прошлая тренировка для подсчета посещаемости
            context.Training.Add(new Training { TrainingId = 2, CoachId = 1, TeamId = 1, FacilityId = 1, DateTraining = yesterday, TimeTraining = new TimeOnly(15, 0) });

            // Посещаемость (50% - 1 был, 1 не был)
            context.Attendances.Add(new Attendance { AttendanceId = 1, TrainingId = 2, StudentId = 1, StatusAttendance = "Был" });
            context.Attendances.Add(new Attendance { AttendanceId = 2, TrainingId = 2, StudentId = 2, StatusAttendance = "Не был" });

            await context.SaveChangesAsync();

            var model = CreateModel(context, coachId: "1");

            // Act
            var result = await model.OnGetAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.CurrentCoach.Should().NotBeNull();
            model.CurrentCoach.NameCoach.Should().Be("Иван");
            model.TodayTrainingsCount.Should().Be(1);
            model.TotalStudents.Should().Be(2);
            model.AverageAttendance.Should().Be("50%");
        }
    }
}