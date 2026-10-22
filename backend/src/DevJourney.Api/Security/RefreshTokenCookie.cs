namespace DevJourney.Api.Security;

/// <summary>
/// O refresh token trafega em cookie HttpOnly + Secure + SameSite=Strict:
/// - HttpOnly: JavaScript não consegue ler (protege contra roubo via XSS);
/// - Secure: só é enviado via HTTPS (navegadores permitem http://localhost em desenvolvimento);
/// - SameSite=Strict: não é enviado em requisições de outros sites (protege contra CSRF);
/// - Path restrito: só é enviado para os endpoints de autenticação.
/// O access token fica apenas na memória do front-end.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "dj_refresh";
    private const string Path = "/api/auth";

    public static void Append(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt,
            IsEssential = true
        });

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, new CookieOptions { Path = Path, Secure = true, SameSite = SameSiteMode.Strict });
}
