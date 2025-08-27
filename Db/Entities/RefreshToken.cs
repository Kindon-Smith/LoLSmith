namespace LoLSmith.Db.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;   // e.g. username or user id
    public string TokenHash { get; set; } = string.Empty; // SHA256 hash of the token
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public string? ReplacedByHash { get; set; }
}