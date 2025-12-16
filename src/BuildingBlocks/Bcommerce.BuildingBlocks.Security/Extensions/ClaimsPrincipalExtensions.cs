using System.Security.Claims;

namespace Bcommerce.BuildingBlocks.Security.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        
        if (claim == null)
        {
            throw new UnauthorizedAccessException("ID do usuário não encontrado no token.");
        }

        if (!Guid.TryParse(claim.Value, out var userId))
        {
            throw new FormatException("O ID do usuário no token não é um GUID válido.");
        }

        return userId;
    }

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.Email) ?? principal.FindFirst("email");
        
        if (claim == null)
        {
            throw new UnauthorizedAccessException("E-mail não encontrado no token.");
        }

        return claim.Value;
    }

    public static List<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }
}
