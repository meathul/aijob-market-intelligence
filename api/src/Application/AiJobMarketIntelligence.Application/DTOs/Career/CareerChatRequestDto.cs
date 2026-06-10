using System.Collections.Generic;

namespace AiJobMarketIntelligence.Application.DTOs.Career;

public sealed class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}

public sealed class CareerChatRequestDto
{
    public string? Message { get; set; }
    public List<ChatMessageDto>? History { get; set; }
}
