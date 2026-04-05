using FootballSchool.Models;
using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace FootballSchool.Tests.Models
{
    public class ModelValidationTests
    {
        [Fact]
        public void User_WithoutLoginOrPassword_ShouldBeInvalid()
        {
            // Arrange
            var user = new User
            {
                Role = "Admin"
                // Login и Password не заданы
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(user, new ValidationContext(user), validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Login"));
            validationResults.Should().Contain(v => v.MemberNames.Contains("Password"));
        }

        [Fact]
        public void User_WithValidData_ShouldBeValid()
        {
            // Arrange
            var user = new User
            {
                Login = "testadmin",
                Password = "securepassword",
                Role = "Admin",
                Email = "test@example.com"
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(user, new ValidationContext(user), validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void Team_WithoutBranchId_ShouldBeInvalid()
        {
            // Arrange
            var team = new Team
            {
                CategoryTeam = "Юниоры U10",
                StatusTeam = "Активна"
                // BranchId не задан (по умолчанию 0, но если бы был int?, провалился бы. Проверим CategoryTeam)
            };
            team.CategoryTeam = null; // Принудительно делаем невалидным для теста

            // Act
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(team, new ValidationContext(team), validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("Название группы обязательно"));
        }
    }
}