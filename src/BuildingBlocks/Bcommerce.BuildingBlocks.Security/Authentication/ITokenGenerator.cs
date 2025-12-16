using System.Security.Claims;

namespace Bcommerce.BuildingBlocks.Security.Authentication;

public interface ITokenGenerator
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<Claim>? customClaims = null);
}
