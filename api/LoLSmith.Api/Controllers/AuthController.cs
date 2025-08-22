using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    public AuthController(IConfiguration config) => _config = config;

    // Dev-only: exchange a username/password for a JWT. Replace with real validation later.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        // very small dev check: use secrets or validate against a user store in prod
        if (req.Username != "demo" || req.Password != "demo") return Unauthorized();

        var key = _config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(key)) return StatusCode(503, "JWT not configured");

        var claims = new[] { new Claim(ClaimTypes.Name, req.Username), new Claim("sub", req.Username) };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { access_token = tokenStr, expires_in = 1800 });
    }

    public record LoginRequest(string Username, string Password);
}