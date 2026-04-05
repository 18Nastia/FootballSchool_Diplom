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
    public class IndexParentModelTests
    {
        private Index_ParentModel CreateModel(FootballSchoolContext context, string userId = null)
        {
            var model = new Index_ParentModel(context);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Parent") };
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

            return model;
        }

        [Fact]
        public async Task OnGetAsync_NoUserIdClaim_RedirectsToLogin()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context, userId: null);

            // Act
            var result = await model.OnGetAsync();

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
            redirectResult.PageName.Should().Be("/Login");
        }

        [Fact]
        public async Task OnGetAsync_ValidParent_LoadsStudentDataAndActiveSubscription()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();

            // Пользователь-родитель
            context.Users.Add(new User { UserId = 1, Login = "parent", Password = "123", Role = "Parent" });

            // Ученик привязан к UserId = 1
            context.Students.Add(new Student { StudentId = 10, UserId = 1, TeamId = 5, NameStudent = "Ребенок", SurnameStudent = "Иванов", GenderStudent = "М", LevelStudent = "Н", ParentNumber = "1", SurnameParent = "Родитель", NameParent = "Иван", CityStudent = "М", StreetStudent = "С", HouseStudent = "1" });

            context.Teams.Add(new Team { TeamId = 5, CategoryTeam = "Команда мечты", BranchId = 1, StatusTeam = "Актив" });

            // Старый и новый абонементы
            context.Subscriptions.Add(new Subscription { SubscriptionId = 1, StudentId = 10, TypeSubscription = "Старый", CostSubscription = 1000 });
            context.Subscriptions.Add(new Subscription { SubscriptionId = 2, StudentId = 10, TypeSubscription = "Новый", CostSubscription = 2000 });

            await context.SaveChangesAsync();

            var model = CreateModel(context, userId: "1");

            // Act
            var result = await model.OnGetAsync();

            // Assert
            result.Should().BeOfType<PageResult>();
            model.StudentData.Should().NotBeNull();
            model.StudentData.NameStudent.Should().Be("Ребенок");
            model.StudentData.Team.CategoryTeam.Should().Be("Команда мечты");

            // Должен загрузить последний добавленный абонемент
            model.ActiveSubscription.Should().NotBeNull();
            model.ActiveSubscription.TypeSubscription.Should().Be("Новый");
        }
    }
}