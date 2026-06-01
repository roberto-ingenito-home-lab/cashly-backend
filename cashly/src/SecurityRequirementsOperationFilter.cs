using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace cashly.src;

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Controlla se l'endpoint ha l'attributo [AllowAnonymous]
        var hasAnonymous =
            (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false)
            || context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (hasAnonymous)
            return; // Se è anonimo, non fare nulla

        // Controlla se l'endpoint ha l'attributo [Authorize] (o a livello di controller)
        var hasAuthorize =
            (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false)
            || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        if (!hasAuthorize)
            return; // Se non è esplicitamente autorizzato, non fare nulla

        // Se l'endpoint è protetto, aggiungi il requisito di sicurezza (il lucchetto)
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                },
            },
        ];
    }
}
