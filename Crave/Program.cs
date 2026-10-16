using Crave.Data;
using Crave.Endpoints;
using Crave.Models;
using Crave.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión a Supabase NO está escrita acá: la guardaste con "dotnet user-secrets".
// (Cuando la app esté publicada, se carga como variable de entorno ConnectionStrings__Crave.)
string? cadena = builder.Configuration.GetConnectionString("Crave");
if (string.IsNullOrWhiteSpace(cadena))
{
    throw new InvalidOperationException("Falta la cadena de conexión. Guardala con: dotnet user-secrets set \"ConnectionStrings:Crave\" \"...\"");
}

// Ningún pedido legítimo pesa más de unos pocos KB. Con este tope, el servidor corta de entrada los envíos gigantes.
builder.WebHost.ConfigureKestrel(opciones => opciones.Limits.MaxRequestBodySize = 64 * 1024);

builder.Services.AddDbContext<CraveDb>(opciones => opciones.UseNpgsql(cadena));
builder.Services.AddScoped<IConfigService, ConfigService>();   // configuración del local (horarios, cupo, abierto/cerrado)
builder.Services.AddScoped<HorarioService>();   // se crea uno nuevo por cada pedido que llega
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<IAdminService, AdminService>();   // lo que usa el panel de administración

// Las claves de la cookie de sesión se guardan en la tabla claves_sesion (ver sql/03-claves-sesion.sql).
builder.Services.AddDataProtection()
    .SetApplicationName("Crave")
    .PersistKeysToDbContext<CraveDb>();
builder.Services.AgregarLimites();   // límites de velocidad por IP (login, pedidos, lectura)
builder.Services.AgregarAdmin();   // login del panel: cookie, autorización y límite de intentos

var app = builder.Build();

app.UsarLimites();   // aplica los límites de velocidad
app.UsarAdmin();   // identifica al dueño en cada pedido y aplica el límite de intentos

app.UseDefaultFiles();   // sirve wwwroot/index.html cuando se entra a "/"
app.UseStaticFiles();    // sirve css, js e imágenes de la carpeta wwwroot

// La página pide la carta acá. Sale de las tablas categorias, productos y zonas.
app.MapGet("/api/menu", async (CraveDb db) =>
{
    var categorias = await db.Categorias
        .Include(c => c.Productos.OrderBy(p => p.Orden))   // trae también los productos de cada categoría
        .OrderBy(c => c.Orden)
        .ToListAsync();
    var zonas = await db.Zonas.OrderBy(z => z.Id).ToListAsync();

    return new { categorias, zonas };
}).RequireRateLimiting(Limites.PoliticaLectura);

// Los horarios disponibles de hoy, con cuántos lugares quedan en cada uno.
app.MapGet("/api/franjas", async (HorarioService horarios) => new
{
    cerrado = !await horarios.LocalAbierto(),   // true cuando el dueño cerró el local desde el panel
    franjas = await horarios.ObtenerFranjas(),
    aviso = await horarios.MensajeSinFranjas()
}).RequireRateLimiting(Limites.PoliticaLectura);

// La página manda el pedido acá. C# convierte el JSON recibido en un objeto PedidoNuevo.
app.MapPost("/api/pedidos", async (PedidoNuevo pedido, PedidoService servicio, HttpContext http, ILoggerFactory logs) =>
{
    // Antispam: si parece mandado por un programa, lo descartamos con un mensaje genérico (sin explicar qué detectamos)
    if (PedidoReglas.EsSospechoso(pedido))
    {
        logs.CreateLogger("Pedidos").LogWarning("Pedido descartado por antispam desde {Ip} (campo oculto lleno: {Trampa}, segundos: {Segundos})",
            http.Connection.RemoteIpAddress, !string.IsNullOrWhiteSpace(pedido.Website), pedido.Segundos);
        return Results.BadRequest(new { errores = new[] { PedidoReglas.MensajeSospechoso } });
    }

    var errores = await servicio.Validar(pedido);
    if (errores.Count > 0)
    {
        return Results.BadRequest(new { errores });   // 400 con la lista de problemas
    }

    var creado = await servicio.Crear(pedido);
    if (creado == null)
    {
        // Alguien se llevó el último lugar de ese horario un segundo antes.
        return Results.Conflict(new { errores = new[] { "Ese horario se acaba de completar. Elegí otro." } });   // 409
    }
    return Results.Ok(creado);                         // 200 con el pedido ya calculado
}).RequireRateLimiting(Limites.PoliticaPedidos);   // máximo 10 envíos cada 10 minutos por IP

app.MapearAdmin();   // /api/admin/login, /api/admin/logout, /api/admin/yo

// Solo para probar mientras desarrollás: ver los últimos pedidos guardados.
// No existe cuando la app se publica, porque muestra datos de clientes.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/pedidos", async (CraveDb db) =>
        await db.Pedidos.Include(p => p.Items).OrderByDescending(p => p.Id).Take(20).ToListAsync());
}

app.Run();
