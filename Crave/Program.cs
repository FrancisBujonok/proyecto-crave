using Crave.Data;
using Crave.Models;
using Crave.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión a Supabase NO está escrita acá: la guardaste con "dotnet user-secrets".
// (Cuando la app esté publicada, se carga como variable de entorno ConnectionStrings__Crave.)
string? cadena = builder.Configuration.GetConnectionString("Crave");
if (string.IsNullOrWhiteSpace(cadena))
{
    throw new InvalidOperationException("Falta la cadena de conexión. Guardala con: dotnet user-secrets set \"ConnectionStrings:Crave\" \"...\"");
}

builder.Services.AddDbContext<CraveDb>(opciones => opciones.UseNpgsql(cadena));
builder.Services.AddScoped<HorarioService>();   // se crea uno nuevo por cada pedido que llega
builder.Services.AddScoped<PedidoService>();

var app = builder.Build();

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
});

// Los horarios disponibles de hoy, con cuántos lugares quedan en cada uno.
app.MapGet("/api/franjas", async (HorarioService horarios) => new
{
    franjas = await horarios.ObtenerFranjas(),
    aviso = HorarioService.MensajeSinFranjas()
});

// La página manda el pedido acá. C# convierte el JSON recibido en un objeto PedidoNuevo.
app.MapPost("/api/pedidos", async (PedidoNuevo pedido, PedidoService servicio) =>
{
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
});

// Solo para probar mientras desarrollás: ver los últimos pedidos guardados.
// No existe cuando la app se publica, porque muestra datos de clientes.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/pedidos", async (CraveDb db) =>
        await db.Pedidos.Include(p => p.Items).OrderByDescending(p => p.Id).Take(20).ToListAsync());
}

app.Run();
