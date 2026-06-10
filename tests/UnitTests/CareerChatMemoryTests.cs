using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiJobMarketIntelligence.Application.Services.Career;
using AiJobMarketIntelligence.Application.Interfaces.Repositories.UserPreferences;
using AiJobMarketIntelligence.Domain.Entities.UserPreferences;

namespace AiJobMarketIntelligence.UnitTests
{
    public class CareerChatMemoryTests
    {
        private static void LoadEnv()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate))
                {
                    foreach (var line in File.ReadAllLines(candidate))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                            continue;
                        
                        var idx = trimmed.IndexOf('=');
                        if (idx > 0)
                        {
                            var key = trimmed.Substring(0, idx).Trim();
                            var val = trimmed.Substring(idx + 1).Trim();
                            Environment.SetEnvironmentVariable(key, val);
                        }
                    }
                    return;
                }
                dir = dir.Parent;
            }
        }

        [Fact]
        public async Task TestChatMemory_WithGroq()
        {
            LoadEnv();
            
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            Assert.NotNull(apiKey);

            var prefsRepoMock = new Mock<IUserJobPreferencesRepository>();
            prefsRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserJobPreferences
                {
                    UserId = "test-user-123",
                    PreferredJobTitle = "Frontend Engineer",
                    Location = "New York",
                    WorkMode = "Any",
                    PreferredSalaryMin = 80000,
                    PreferredSalaryMax = 120000,
                    SkillsText = "React, TypeScript, CSS"
                });

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["GROQ_API_KEY"]).Returns(apiKey);

            var loggerMock = new Mock<ILogger<CareerChatService>>();

            var service = new CareerChatService(prefsRepoMock.Object, configMock.Object, loggerMock.Object);

            var userId = "test-user-123";

            // First turn
            var response1 = await service.AskAsync(userId, "my name is athul");
            Console.WriteLine($"[Turn 1 Response]: {response1.Answer}");

            // Second turn
            var response2 = await service.AskAsync(userId, "What did i tell my name was?");
            Console.WriteLine($"[Turn 2 Response]: {response2.Answer}");

            // We expect the LLM to know the user's name is athul
            Assert.Contains("athul", response1.Answer.ToLowerInvariant());
            Assert.Contains("athul", response2.Answer.ToLowerInvariant());
        }

        [Fact]
        public async Task TestChatMemory_Stateless_WithGroq()
        {
            LoadEnv();
            
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            Assert.NotNull(apiKey);

            var prefsRepoMock = new Mock<IUserJobPreferencesRepository>();
            prefsRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new UserJobPreferences
                {
                    UserId = "test-user-123",
                    PreferredJobTitle = "Frontend Engineer",
                    Location = "New York",
                    WorkMode = "Any",
                    PreferredSalaryMin = 80000,
                    PreferredSalaryMax = 120000,
                    SkillsText = "React, TypeScript, CSS"
                });

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["GROQ_API_KEY"]).Returns(apiKey);

            var loggerMock = new Mock<ILogger<CareerChatService>>();

            var service = new CareerChatService(prefsRepoMock.Object, configMock.Object, loggerMock.Object);

            var userId = "test-user-123";

            // Turn 1: User says "my name is athul"
            var response1 = await service.AskAsync(userId, "my name is athul", null);
            Console.WriteLine($"[Stateless Turn 1 Response]: {response1.Answer}");

            // Construct history manually to pass to Turn 2
            var history = new System.Collections.Generic.List<AiJobMarketIntelligence.Application.DTOs.Career.ChatMessageDto>
            {
                new AiJobMarketIntelligence.Application.DTOs.Career.ChatMessageDto { Role = "user", Content = "my name is athul" },
                new AiJobMarketIntelligence.Application.DTOs.Career.ChatMessageDto { Role = "assistant", Content = response1.Answer }
            };

            // Turn 2: User asks "What did i tell my name was?" and we pass the history
            var response2 = await service.AskAsync(userId, "What did i tell my name was?", history);
            Console.WriteLine($"[Stateless Turn 2 Response]: {response2.Answer}");

            Assert.Contains("athul", response1.Answer.ToLowerInvariant());
            Assert.Contains("athul", response2.Answer.ToLowerInvariant());
        }
    }
}
