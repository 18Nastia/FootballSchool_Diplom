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

namespace FootballSchool.Tests.Pages
{
    public class BranchesModelTests
    {
        private BranchesModel CreateModel(FootballSchoolContext context, string role = "Admin")
        {
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(m => m.WebRootPath).Returns("C:\\TestPath\\wwwroot");

            var model = new BranchesModel(context, mockEnvironment.Object);

            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Настройка IObjectModelValidator
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
        public async Task OnPostSaveBranchAsync_AddsNewBranch()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            var model = CreateModel(context);
            model.ModalBranch = new Branch
            {
                NameBranch = "Филиал Тест",
                CityBranch = "Москва",
                StreetBranch = "Мира",
                HouseBranch = "12",
                PhoneBranch = "+79991234567"
            };

            // Act
            var result = await model.OnPostSaveBranchAsync(null);

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var dbBranch = await context.Branches.FirstOrDefaultAsync(b => b.NameBranch == "Филиал Тест");
            dbBranch.Should().NotBeNull();
            dbBranch.CityBranch.Should().Be("Москва");
        }

        [Fact]
        public async Task OnPostSaveFacilityAsync_AddsNewFacility()
        {
            // Arrange
            using var context = TestHelper.GetInMemoryDbContext();
            context.Branches.Add(new Branch { BranchId = 1, NameBranch = "Филиал 1", CityBranch = "Мск", StreetBranch = "С", HouseBranch = "1", PhoneBranch = "1" });
            await context.SaveChangesAsync();

            var model = CreateModel(context);
            model.ModalFacility = new Facility
            {
                BranchId = 1,
                NameFacility = "Большой манеж",
                TypeFacility = "Манеж",
                CapacityFacility = 30,
                StatusFacility = "Активен"
            };

            // Act
            var result = await model.OnPostSaveFacilityAsync();

            // Assert
            result.Should().BeOfType<RedirectToPageResult>();
            var dbFacility = await context.Facilities.FirstOrDefaultAsync(f => f.NameFacility == "Большой манеж");
            dbFacility.Should().NotBeNull();
            dbFacility.CapacityFacility.Should().Be(30);
        }
    }
}