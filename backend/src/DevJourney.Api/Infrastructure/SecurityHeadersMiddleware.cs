namespace DevJourney.Api.Infrastructure;

/// <summary>
/// Cabeçalhos HTTP de segurança recomendados pela OWASP para APIs.
/// </summary>
internal static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            // A API não serve HTML; a documentação (Scalar) fica fora desta política.
            if (!context.Request.Path.StartsWithSegments("/scalar"))
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            await next();
        });
}
