using FootballSchool.Models;
using FootballSchool.Pages.Main_Pages;
using FootballSchool.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FootballSchool.Tests.Pages
{
    public class IndexAdminModelTests
    {
        [Fact]
        public async Task OnGetAsync_CalculatesStatisticsCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<Index_AdminModel>>();
            var model = new Index_AdminModel(context, mockLogger.Object);

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Seed Data: 2 Students
            context.Students.Add(new Student { StudentId = 1, NameStudent = "John", SurnameStudent = "Doe", GenderStudent = "Мужской", LevelStudent = "Новичок", ParentNumber = "1", SurnameParent = "P", NameParent = "N", CityStudent = "C", StreetStudent = "S", HouseStudent = "H" });
            context.Students.Add(new Student { StudentId = 2, NameStudent = "Jane", SurnameStudent = "Doe", GenderStudent = "Женский", LevelStudent = "Новичок", ParentNumber = "1", SurnameParent = "P", NameParent = "N", CityStudent = "C", StreetStudent = "S", HouseStudent = "H" });

            // Seed Data: 1 Training Today
            context.Training.Add(new Training { TrainingId = 1, DateTraining = today, TimeTraining = new TimeOnly(10, 0), CoachId = 1, TeamId = 1, FacilityId = 1 });

            // Seed Data: 1 Active Subscription
            context.Subscriptions.Add(new Subscription { SubscriptionId = 1, StudentId = 1, TypeSubscription = "Test" });

            // Seed Data: 1 Pending Payment
            context.Payments.Add(new Payment { PaymentId = 1, SubscriptionId = 1, StatusPayment = "В обработке", MethodPayment = "Cash" });

            await context.SaveChangesAsync();

            // Act
            await model.OnGetAsync();

            // Assert
            model.TotalStudentsCount.Should().Be(2);
            model.TodayTrainingsCount.Should().Be(1);
            model.ActiveSubscriptionsCount.Should().Be(1);
            model.PendingPaymentsCount.Should().Be(1);
            model.WeeklyAttendancePercentage.Should().Be(0); // Нет записей о посещаемости
        }

        [Fact]
        public async Task OnGetAsync_CalculatesAttendancePercentageCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var mockLogger = new Mock<ILogger<Index_AdminModel>>();
            var model = new Index_AdminModel(context, mockLogger.Object);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var training = new Training { TrainingId = 1, DateTraining = today, TimeTraining = new TimeOnly(10, 0), CoachId = 1, TeamId = 1, FacilityId = 1 };
            context.Training.Add(training);

            // 3 присутствуют, 1 отсутствует (75%)
            context.Attendances.Add(new Attendance { AttendanceId = 1, TrainingId = 1, StudentId = 1, StatusAttendance = "Был" });
            context.Attendances.Add(new Attendance { AttendanceId = 2, TrainingId = 1, StudentId = 2, StatusAttendance = "Был" });
            context.Attendances.Add(new Attendance { AttendanceId = 3, TrainingId = 1, StudentId = 3, StatusAttendance = "Был" });
            context.Attendances.Add(new Attendance { AttendanceId = 4, TrainingId = 1, StudentId = 4, StatusAttendance = "Не был" });

            await context.SaveChangesAsync();

            // Act
            await model.OnGetAsync();

            // Assert
            model.WeeklyAttendancePercentage.Should().Be(75);
        }
    }
}