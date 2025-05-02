using Microsoft.AspNetCore.Mvc;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using SignalRChatServer.Services;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginAuthRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            var user = await _userRepository.AuthenticateAsync(request.Username, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized(new { Message = "Invalid credentials" });
            }

            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var token = _authService.GenerateJwtToken(user);

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, new { Message = "An error occurred while processing your request" });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterAuthRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required" });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new { Message = "Password must be at least 8 characters long" });
            }

            if (await _userRepository.ExistsAsync(request.Username))
            {
                return Conflict(new { Message = "Username already exists" });
            }

            var (hash, salt) = _authService.CreatePasswordHash(request.Password);

            var user = new UserModel
            {
                Username = request.Username,
                PasswordHash = hash,
                Salt = salt,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            var token = _authService.GenerateJwtToken(user);

            _logger.LogInformation("New user registered: {Username}", user.Username);

            return Ok(new AuthResponse
            {
                Token = token,
                Username = user.Username,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return StatusCode(500, new { Message = "An error occurred while processing your request" });
        }
    }
}