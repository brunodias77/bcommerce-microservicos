using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserService.Domain.Aggregates;

namespace UserService.Domain.Entities;

[Table("revoked_jwt_tokens")]
public class RevokedJwtToken
{
    [Key]
    [Column("jti")]
    public Guid Jti { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}