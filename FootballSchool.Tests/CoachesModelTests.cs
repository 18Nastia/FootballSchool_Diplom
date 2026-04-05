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
using System;

namespace FootballSchool.Tests.Pages
{
    public class CoachesModelTests
    {
        private CoachesModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath\\wwwroot");

            var model = new CoachesModel(context, mockEnvironment.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Настройка IObjectModelValidator для корректной работы TryValidateModel
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
        public async Task OnGetAsync_PopulatesCoachesAndSpecialtiesCorrectly()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Тренер без тренировок (Свободен)
            context.Coaches.Add(new Coach { CoachId = 1, NameCoach = "Олег", SurnameCoach = "Свободный", QualificationCoach = "КМС", SpecialtyCoach = "ОФП" });

            // Тренер с тренировкой (Занят)
            context.Coaches.Add(new Coach { CoachId = 2, NameCoach = "Иван", SurnameCoach = "Занятой", QualificationCoach = "PRO", SpecialtyCoach = "Тактика" });
            context.Training.Add(new Training { TrainingId = 1, CoachId = 2, TeamId = 1, FacilityId = 1, DateTraining = DateOnly.FromDateTime(DateTime.Today), TimeTraining = new TimeOnly(12, 0) });

            await context.SaveChangesAsync();

            var model = CreateModel(context);

            // Act
            await model.OnGetAsync();

            // Assert
            model.CoachesList.Should().HaveCount(2);
            model.Specialties.Should().Contain("ОФП");
            model.Specialties.Should().Contain("Тактика");

            var freeCoach = model.CoachesList.Find(c => c.CoachId == 1);
            freeCoach.StatusText.Should().Be("Свободен");
            freeCoach.StatusClass.Should().Be("status-free");

            var busyCoach = model.CoachesList.Find(c => c.CoachId == 2);
            busyCoach.StatusText.Should().Be("Занят");
            busyCoach.StatusClass.Should().Be("status-busy");
        }

        [Fact]
        public async Task OnPostAddCoachAsync_ValidData_CreatesCoachAndLinkedUserAccount()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);

            model.NewCoach = new Coach
            {
                NameCoach = "Сергей",
                SurnameCoach = "Семак",
                QualificationCoach = "PRO License",
                SpecialtyCoach = "Главный тренер"
            };

            // Act
            var result = await model.OnPostAddCoachAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            // Проверяем создание тренера
            var coachInDb = await context.Coaches.FirstOrDefaultAsync(c => c.NameCoach == "Сергей");
            coachInDb.Should().NotBeNull();
            coachInDb.UserId.Should().NotBeNull();

            // Проверяем создание пользователя с правильной ролью
            var userInDb = await context.Users.FindAsync(coachInDb.UserId);
            userInDb.Should().NotBeNull();
            userInDb.Role.Should().Be("Coach");
            userInDb.Login.Should().StartWith("coach_сергей");

            // Пароль должен был сгенерироваться автоматически (длина 12 символов)
            userInDb.Password.Should().NotBeNullOrEmpty();
            userInDb.Password.Length.Should().Be(12);

            model.TempData["SuccessMessage"].ToString().Should().Contain("успешно добавлен");
        }

        [Fact]
        public async Task OnPostDeleteCoachAsync_AdminRole_CascadesDeletesCoachAndUser()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Создаем юзера, тренера и связанную с ним тренировку
            var user = new User { UserId = 10, Login = "coach_del", Password = "123", Role = "Coach" };
            var coach = new Coach { CoachId = 5, UserId = 10, NameCoach = "Удаляемый", SurnameCoach = "Тренер", QualificationCoach = "КМС", SpecialtyCoach = "Бег" };
            var training = new Training { TrainingId = 1, CoachId = 5, TeamId = 1, FacilityId = 1, DateTraining = DateOnly.FromDateTime(DateTime.Today) };

            context.Users.Add(user);
            context.Coaches.Add(coach);
            context.Training.Add(training);
            await context.SaveChangesAsync();

            var model = CreateModel(context, role: "Admin");

            // Act
            var result = await model.OnPostDeleteCoachAsync(5);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();

            var deletedCoach = await context.Coaches.FindAsync(5);
            deletedCoach.Should().BeNull(); // Тренер удален

            var deletedUser = await context.Users.FindAsync(10);
            deletedUser.Should().BeNull(); // Связанный аккаунт удален

            var deletedTraining = await context.Training.FindAsync(1);
            deletedTraining.Should().BeNull(); // Тренировки тренера удалены
        }
    }
}