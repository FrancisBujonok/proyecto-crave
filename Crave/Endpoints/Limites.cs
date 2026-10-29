using System.Threading.RateLimiting;

namespace Crave.Endpoints;

// Límites de velocidad: cuántos pedidos HTTP acepta el servidor de una misma persona (identificada por su IP).
// Si se pasa, el servidor responde 429 "Too Many Requests" sin siquiera ejecutar el endpoint.
// Sirve contra quien prueba contraseñas, quien manda pedidos falsos sin parar o quien satura la base de datos.
//
// OJO con los celulares: Personal, Claro y Movistar comparten UNA misma IP entre muchos clientes (CGNAT).
// Por eso los límites de pedidos y de lectura son generosos: tienen que aguantar a varios vecinos pidiendo
// la misma noche desde la misma IP. Contra el spam están además el tope por teléfono y el campo trampa (PedidoReglas).
public static class Limites
{
    public const string PoliticaLogin = "login-admin";   // 5 intentos de login cada 10 minutos
    public const string PoliticaPedidos = "pedidos";     // 30 envíos de pedido cada 10 minutos
    public const string PoliticaLectura = "lectura";     // 200 lecturas de la carta o los horarios por minuto
    public const string PoliticaSeguimiento = "seguimiento";   // 120 consultas de seguimiento de pedido por minuto

    // Una regla "máximo N cada tanto por IP"
    static RateLimitPartition<string> PorIp(HttpContext contexto, int maximo, TimeSpan ventana)
    {
        string ip = contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip";   // cada IP tiene su propio contador
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = maximo,
            Window = ventana,
            QueueLimit = 0   // los que se pasan no esperan en fila: se rechazan
        });
    }

    // Se llama una vez al armar la app (builder.Services.AgregarLimites())
    public static IServiceCollection AgregarLimites(this IServiceCollection servicios)
    {
        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opciones.AddPolicy(PoliticaLogin, contexto => PorIp(contexto, 5, TimeSpan.FromMinutes(10)));
            opciones.AddPolicy(PoliticaPedidos, contexto => PorIp(contexto, 30, TimeSpan.FromMinutes(10)));
            opciones.AddPolicy(PoliticaLectura, contexto => PorIp(contexto, 200, TimeSpan.FromMinutes(1)));
            // La página de seguimiento pregunta cada 15 segundos (4 por minuto por cliente): 120 alcanza para unas 30 personas
            // detrás de la misma IP del celular. Probar códigos al azar sigue siendo inútil (más de 7 x 10^17 combinaciones).
            opciones.AddPolicy(PoliticaSeguimiento, contexto => PorIp(contexto, 120, TimeSpan.FromMinutes(1)));

            // Qué se responde cuando alguien se pasa. "error" lo lee el login del panel y "errores" lo lee la tienda.
            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                var http = contexto.HttpContext;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    http.Response.Headers.RetryAfter = ((int)espera.TotalSeconds).ToString();
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                string mensaje = http.Request.Path.StartsWithSegments("/api/pedidos")
                    ? "Enviaste demasiados pedidos seguidos. Esperá unos minutos y probá de nuevo."
                    : "Demasiados intentos. Esperá unos minutos y probá de nuevo.";
                await http.Response.WriteAsJsonAsync(new { error = mensaje, errores = new[] { mensaje } }, cancelacion);
            };
        });
        return servicios;
    }

    // Se llama una vez después de construir la app (app.UsarLimites())
    public static WebApplication UsarLimites(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }
}
