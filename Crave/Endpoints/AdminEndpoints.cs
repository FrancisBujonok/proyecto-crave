using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Crave.Endpoints;

// Todo lo del panel de administración que tiene que ver con entrar y salir.
// La idea: el dueño escribe UNA contraseña (guardada fuera del código) y el servidor le da una cookie de sesión.
// En cada pedido siguiente el navegador manda la cookie sola, y el servidor sabe que es el dueño.

// Lo que manda la página de login (JSON -> C#)
public record LoginAdmin(string? Password);

public static class AdminEndpoints
{
    const string PoliticaLogin = "login-admin";   // nombre de la regla "máximo de intentos de login"
    const int LargoMinimoContrasena = 10;         // una contraseña más corta se considera "no configurada"

    // ---------- 1) SERVICIOS: se llama una vez al armar la app (builder.Services.AgregarAdmin()) ----------
    public static IServiceCollection AgregarAdmin(this IServiceCollection servicios)
    {
        // Autenticación por cookie: ASP.NET firma la cookie y después sabe reconocerla.
        servicios.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opciones =>
            {
                opciones.Cookie.Name = "crave_admin";
                opciones.Cookie.HttpOnly = true;                                  // el JavaScript de la página no puede leerla
                opciones.Cookie.SameSite = SameSiteMode.Strict;                   // el navegador solo la manda desde nuestro propio sitio
                opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // con https va "Secure"; en localhost (http) también funciona
                opciones.ExpireTimeSpan = TimeSpan.FromDays(7);                   // dura 7 días...
                opciones.SlidingExpiration = true;                                // ...y se renueva mientras el dueño la use

                // Es una API, no un sitio con página de login: sin sesión respondemos 401 en vez de redirigir.
                opciones.Events.OnRedirectToLogin = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                opciones.Events.OnRedirectToAccessDenied = contexto =>
                {
                    contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        servicios.AddAuthorization();

        // Límite de intentos: máximo 5 pedidos de login cada 10 minutos desde la misma IP.
        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.AddPolicy(PoliticaLogin, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",   // cada IP tiene su propio contador
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0   // los que se pasan no esperan en fila: se rechazan
                }));

            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    contexto.HttpContext.Response.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString();
                contexto.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await contexto.HttpContext.Response.WriteAsJsonAsync(new { error = "Demasiados intentos. Esperá unos minutos y probá de nuevo." }, cancelacion);
            };
        });

        return servicios;
    }

    // ---------- 2) MIDDLEWARE: se llama una vez después de construir la app (app.UsarAdmin()) ----------
    // El orden importa: primero se identifica quién es (Authentication), después se decide si puede pasar (Authorization).
    public static WebApplication UsarAdmin(this WebApplication app)
    {
        // Las respuestas del panel tienen datos privados: que ningún navegador ni proxy las guarde.
        app.Use(async (contexto, siguiente) =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api/admin"))
                contexto.Response.Headers.CacheControl = "no-store";
            await siguiente();
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        return app;
    }

    // ---------- 3) DIRECCIONES: app.MapearAdmin() ----------
    public static void MapearAdmin(this WebApplication app)
    {
        // Todo lo que se agregue a este grupo queda PROTEGIDO: sin sesión, el servidor responde 401.
        // Las únicas excepciones son las que dicen AllowAnonymous (entrar y salir).
        var admin = app.MapGroup("/api/admin").RequireAuthorization();

        admin.MapPost("/login", Login).AllowAnonymous().RequireRateLimiting(PoliticaLogin);

        admin.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync();   // borra la cookie de sesión
            return Results.Ok(new { ok = true });
        }).AllowAnonymous();

        // La página lo usa para saber si ya hay sesión iniciada: 200 si sí, 401 si no.
        admin.MapGet("/yo", () => Results.Ok(new { autenticado = true }));
    }

    static async Task<IResult> Login(LoginAdmin datos, HttpContext http, IConfiguration configuracion, ILoggerFactory fabricaLogs)
    {
        var log = fabricaLogs.CreateLogger("Admin");

        // La contraseña real vive en la configuración: user-secrets en tu PC, variable de entorno Admin__Password en Render.
        string? esperada = configuracion["Admin:Password"];
        if (string.IsNullOrWhiteSpace(esperada) || esperada.Length < LargoMinimoContrasena)
        {
            log.LogWarning("Panel desactivado: falta Admin:Password o tiene menos de {Largo} caracteres.", LargoMinimoContrasena);
            return Results.Json(new { error = "El panel todavía no está configurado." }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        string ingresada = datos.Password ?? "";
        if (ingresada.Length > 200 || !ContrasenaCorrecta(ingresada, esperada))
        {
            log.LogWarning("Intento de login fallido desde {Ip}", http.Connection.RemoteIpAddress);   // la contraseña NUNCA se escribe en el log
            await Task.Delay(400);   // frena un poco a quien prueba contraseñas
            return Results.Json(new { error = "Contraseña incorrecta." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        // Contraseña correcta: armamos la "identidad" del dueño y le entregamos la cookie.
        var identidad = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") },
            CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });   // sigue iniciada aunque se cierre el navegador

        return Results.Ok(new { ok = true });
    }

    // Compara las contraseñas sin que el tiempo que tarda revele cuántas letras coincidieron.
    // Se comparan los "hashes" SHA-256 (siempre del mismo largo), no los textos directamente.
    static bool ContrasenaCorrecta(string ingresada, string esperada)
    {
        byte[] a = SHA256.HashData(Encoding.UTF8.GetBytes(ingresada));
        byte[] b = SHA256.HashData(Encoding.UTF8.GetBytes(esperada));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
