using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using LoLSmith.Db;
using LoLSmith.Db.Entities;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly LoLSmithDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AuthController(IConfiguration config, LoLSmithDbContext db, IWebHostEnvironment env)
    {
        _config = config;
        _db = db;
        _env = env;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // validate against configured admin creds for demo/public small app
        var adminUser = _config["Auth:AdminUser"];
        var adminPass = _config["Auth:AdminPass"];
        var jwtKey = _config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            return StatusCode(503, new { error = "JWT not configured" });

        if (string.IsNullOrWhiteSpace(adminUser) || string.IsNullOrWhiteSpace(adminPass))
            return StatusCode(503, new { error = "Admin credentials not configured" });

        if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Username and Password are required" });

        if (req.Username != adminUser || req.Password != adminPass)
            return Unauthorized(new { error = "Invalid username or password" });

        var accessToken = GenerateJwt(req.Username, jwtKey, TimeSpan.FromMinutes(15));
        var refreshToken = GenerateRandomToken();
        var refreshHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;
        var rt = new RefreshToken
        {
            UserId = req.Username,
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(30),
            Revoked = false
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();

        SetRefreshCookie(refreshToken, rt.ExpiresAt);

        return Ok(new { access_token = accessToken, expires_in = 900 });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var jwtKey = _config["Auth:JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            return StatusCode(503, new { error = "JWT not configured" });

        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new { error = "Missing refresh token cookie" });

        var refreshHash = HashToken(refreshToken);
        var tokenEntry = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == refreshHash);
        if (tokenEntry == null || tokenEntry.Revoked || tokenEntry.ExpiresAt <= DateTime.UtcNow)
            return Unauthorized(new { error = "Invalid or expired refresh token" });

        // rotate
        var newRefresh = GenerateRandomToken();
        var newHash = HashToken(newRefresh);
        tokenEntry.Revoked = true;
        tokenEntry.ReplacedByHash = newHash;

        var now = DateTime.UtcNow;
        var newEntry = new RefreshToken
        {
            UserId = tokenEntry.UserId,
            TokenHash = newHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(30),
            Revoked = false
        };
        _db.RefreshTokens.Add(newEntry);
        await _db.SaveChangesAsync();

        SetRefreshCookie(newRefresh, newEntry.ExpiresAt);

        var accessToken = GenerateJwt(tokenEntry.UserId, jwtKey, TimeSpan.FromMinutes(15));
        return Ok(new { access_token = accessToken, expires_in = 900 });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var refreshHash = HashToken(refreshToken);
            var tokenEntry = await _db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == refreshHash);
            if (tokenEntry != null)
            {
                tokenEntry.Revoked = true;
                await _db.SaveChangesAsync();
            }
        }

        // remove cookie
        Response.Cookies.Append("refreshToken", "", new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax
        });

        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
        return Ok(new { name = User.Identity?.Name, claims = User.Claims.Select(c => new { c.Type, c.Value }) });
    }

    // helpers
    private string GenerateJwt(string username, string jwtKey, TimeSpan ttl)
    {
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("sub", username)
        };
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.Add(ttl), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRandomToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash); // .NET 5+; hex uppercase is fine
    }

    private void SetRefreshCookie(string token, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = expires,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}

[ApiController]
[Route("api/debug/auth")]
public class AuthDebugController : ControllerBase
{
    private readonly LoLSmithDbContext _db;
    public AuthDebugController(LoLSmithDbContext db) => _db = db;

    [HttpGet("refresh-tokens")]
    public async Task<IActionResult> GetRefreshTokens() =>
        Ok(await _db.RefreshTokens.OrderByDescending(r=>r.CreatedAt).Take(20).Select(r => new {r.Id, r.UserId, r.CreatedAt, r.ExpiresAt, r.Revoked}).ToListAsync());
}