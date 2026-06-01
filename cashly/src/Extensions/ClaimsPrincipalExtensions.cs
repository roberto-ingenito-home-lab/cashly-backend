using System.Security.Claims;

namespace cashly.src.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        // Trova la claim 'sub' 
        var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdValue, out int userId))
        {
            return userId;
        }

        // Lancia un'eccezione se il token è valido ma non contiene un ID utente valido.
        // Questo indica un problema nella generazione del token.
        throw new InvalidOperationException("L'ID utente non è presente o non è valido nel token.");
    }
}