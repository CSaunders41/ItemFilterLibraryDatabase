using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ItemFilterLibraryAPI.Models;
using ItemFilterLibraryAPI.Services;

namespace ItemFilterLibraryAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authService;

    public AuthController(AuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpGet("discord/login")]
    public IActionResult DiscordLogin()
    {
        // For simplified local authentication, return a basic login page
        var html = """
            <!DOCTYPE html>
            <html>
            <head>
                <title>ItemFilterLibrary Local Login</title>
                <style>
                    body { font-family: Arial, sans-serif; background: #f0f0f0; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
                    .login-container { background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                    input { width: 100%; padding: 10px; margin: 10px 0; border: 1px solid #ddd; border-radius: 5px; box-sizing: border-box; }
                    button { width: 100%; padding: 10px; background: #007bff; color: white; border: none; border-radius: 5px; cursor: pointer; }
                    button:hover { background: #0056b3; }
                    .token-result { margin-top: 20px; padding: 10px; background: #f8f9fa; border-radius: 5px; border: 1px solid #dee2e6; }
                    .token-result textarea { width: 100%; height: 100px; margin-top: 10px; }
                </style>
            </head>
            <body>
                <div class="login-container">
                    <h2>ItemFilterLibrary Local Login</h2>
                    <form id="loginForm">
                        <input type="text" id="username" placeholder="Username" required>
                        <input type="password" id="password" placeholder="Password (optional for local use)" value="password">
                        <button type="submit">Login</button>
                    </form>
                    <div id="tokenResult" class="token-result" style="display: none;">
                        <h3>Auth Token Generated:</h3>
                        <p>Copy this token and paste it in the plugin:</p>
                        <textarea id="tokenText" readonly></textarea>
                    </div>
                </div>
                <script>
                    document.getElementById('loginForm').addEventListener('submit', async (e) => {
                        e.preventDefault();
                        const username = document.getElementById('username').value;
                        const password = document.getElementById('password').value;
                        
                        try {
                            const response = await fetch('/auth/login', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ username, password })
                            });
                            
                            if (response.ok) {
                                const data = await response.json();
                                document.getElementById('tokenText').value = data.token;
                                document.getElementById('tokenResult').style.display = 'block';
                            } else {
                                alert('Login failed');
                            }
                        } catch (error) {
                            alert('Error: ' + error.message);
                        }
                    });
                </script>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var authData = await _authService.LoginAsync(request.Username, request.Password);
            var token = _authService.GenerateLoginToken(authData.User!);
            
            return Ok(new { token });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var authData = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(new ApiResponse<AuthData> { Data = authData, Success = true });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid refresh token" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("test")]
    [Authorize]
    public async Task<IActionResult> TestAuth()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            var response = new TestAuthResponse
            {
                Status = "connected",
                User = user
            };

            return Ok(new ApiResponse<TestAuthResponse> { Data = response, Success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
} 