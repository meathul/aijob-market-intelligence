using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using AiJobMarketIntelligence.Api.Controllers.User;
using AiJobMarketIntelligence.Application.DTOs.UserPreferences;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;

namespace AiJobMarketIntelligence.UnitTests
{
    public class UserPreferencesControllerTests
    {
        private readonly Mock<IUserJobPreferencesRepository> _repoMock = new();
        private readonly UserPreferencesController _controller;
        private const string TestUserId = "user-12345";

        public UserPreferencesControllerTests()
        {
            _controller = new UserPreferencesController(_repoMock.Object);
            SetupControllerUser(TestUserId);
        }

        private void SetupControllerUser(string? userId)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            if (userId != null)
            {
                claimsPrincipal.AddIdentity(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "mock"));
            }

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void GetMethod_HasResponseCacheNoStoreAttribute()
        {
            // Arrange
            var method = typeof(UserPreferencesController).GetMethod(nameof(UserPreferencesController.Get));

            // Act
            var attribute = method?.GetCustomAttribute<ResponseCacheAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.True(attribute.NoStore);
            Assert.Equal(ResponseCacheLocation.None, attribute.Location);
        }

        [Fact]
        public async Task Get_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(null);

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Get_NoPreferencesExist_ReturnsEmptyPreferencesWithOnboardingCompletedFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByUserIdAsync(TestUserId)).ReturnsAsync((UserJobPreferences?)null);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserJobPreferencesDto>(okResult.Value);
            Assert.False(dto.OnboardingCompleted);
            Assert.Null(dto.Location);
        }

        [Fact]
        public async Task Get_PreferencesExist_ReturnsMappedDto()
        {
            // Arrange
            var entity = new UserJobPreferences
            {
                UserId = TestUserId,
                Location = "New York",
                PreferredJobTitle = "Staff Engineer",
                PreferredSalaryMin = 120000,
                PreferredSalaryMax = 180000,
                WorkMode = "Remote",
                SkillsText = "C#, Angular",
                OnboardingCompleted = true
            };
            _repoMock.Setup(r => r.GetByUserIdAsync(TestUserId)).ReturnsAsync(entity);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserJobPreferencesDto>(okResult.Value);
            Assert.True(dto.OnboardingCompleted);
            Assert.Equal("New York", dto.Location);
            Assert.Equal("Staff Engineer", dto.PreferredJobTitle);
            Assert.Equal(120000, dto.PreferredSalaryMin);
            Assert.Equal(180000, dto.PreferredSalaryMax);
            Assert.Equal("Remote", dto.WorkMode);
            Assert.Equal("C#, Angular", dto.SkillsText);
        }

        [Fact]
        public async Task Upsert_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupControllerUser(null);
            var request = new UserJobPreferencesDto { Location = "New York", PreferredJobTitle = "Engineer" };

            // Act
            var result = await _controller.Upsert(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Upsert_NegativeSalaryMin_ReturnsBadRequest()
        {
            // Arrange
            var request = new UserJobPreferencesDto { PreferredSalaryMin = -1000 };

            // Act
            var result = await _controller.Upsert(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("PreferredSalaryMin must be >= 0", message);
        }

        [Fact]
        public async Task Upsert_SalaryMinGreaterThanSalaryMax_ReturnsBadRequest()
        {
            // Arrange
            var request = new UserJobPreferencesDto { PreferredSalaryMin = 150000, PreferredSalaryMax = 100000 };

            // Act
            var result = await _controller.Upsert(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("PreferredSalaryMin must be <= PreferredSalaryMax", message);
        }

        [Fact]
        public async Task Upsert_NoMeaningfulPreferences_ReturnsBadRequest()
        {
            // Arrange
            var request = new UserJobPreferencesDto { WorkMode = "Any" }; // "Any" only is not considered meaningful in our rules

            // Act
            var result = await _controller.Upsert(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var message = badRequestResult.Value?.GetType().GetProperty("message")?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Contains("Add at least one preference", message);
        }

        [Fact]
        public async Task Upsert_ValidPreferences_UpsertsAndReturnsMappedDto()
        {
            // Arrange
            var request = new UserJobPreferencesDto
            {
                Location = "Austin",
                PreferredJobTitle = "Lead Dev",
                PreferredSalaryMin = 100000,
                PreferredSalaryMax = 150000,
                WorkMode = "Remote",
                SkillsText = "C#"
            };

            var savedEntity = new UserJobPreferences
            {
                UserId = TestUserId,
                Location = "Austin",
                PreferredJobTitle = "Lead Dev",
                PreferredSalaryMin = 100000,
                PreferredSalaryMax = 150000,
                WorkMode = "Remote",
                SkillsText = "C#",
                OnboardingCompleted = true
            };

            _repoMock.Setup(r => r.UpsertAsync(It.Is<UserJobPreferences>(p =>
                p.UserId == TestUserId &&
                p.Location == "Austin" &&
                p.PreferredJobTitle == "Lead Dev" &&
                p.PreferredSalaryMin == 100000 &&
                p.PreferredSalaryMax == 150000 &&
                p.WorkMode == "Remote" &&
                p.SkillsText == "C#"
            ))).ReturnsAsync(savedEntity);

            // Act
            var result = await _controller.Upsert(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UserJobPreferencesDto>(okResult.Value);
            Assert.True(dto.OnboardingCompleted);
            Assert.Equal("Austin", dto.Location);
            Assert.Equal("Lead Dev", dto.PreferredJobTitle);
        }
    }
}
