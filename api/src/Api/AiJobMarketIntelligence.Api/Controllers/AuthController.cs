using AiJobMarketIntelligence.Api.Models.Auth;
using AiJobMarketIntelligence.Api.Services;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AiJobMarketIntelligence.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "Email already registered" });

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Registration failed", errors = result.Errors.Select(e => e.Description).ToList() });

        // Default role
        await _userManager.AddToRoleAsync(user, "User");

        var (token, expiresAt, roles) = await _jwtTokenService.CreateTokenAsync(user);
        return Ok(new AuthResponse(token, expiresAt, user.Email!, roles));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials" });

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
            return Unauthorized(new { message = "Invalid credentials" });

        var (token, expiresAt, roles) = await _jwtTokenService.CreateTokenAsync(user);
        return Ok(new AuthResponse(token, expiresAt, user.Email!, roles));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst("email")?.Value,
            roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToArray()
        });
    }
}
