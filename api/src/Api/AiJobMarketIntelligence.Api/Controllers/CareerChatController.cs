using AiJobMarketIntelligence.Application.DTOs.Career;
using AiJobMarketIntelligence.Application.Interfaces.Services.Career;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CareerChatController : ControllerBase
{
    private const int MaxMessageLength = 2000;

    private readonly ICareerChatService _careerChat;

    public CareerChatController(ICareerChatService careerChat)
    {
        _careerChat = careerChat;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CareerChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CareerChatResponseDto>> Ask([FromBody] CareerChatRequestDto request)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { message = "Message is required." });

        if (message.Length > MaxMessageLength)
            return BadRequest(new { message = $"Message must be {MaxMessageLength} characters or fewer." });

        var userId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        return Ok(await _careerChat.AskAsync(userId, message));
    }
}
