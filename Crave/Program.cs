using Crave.Data;
using Crave.Models;
using Crave.Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();   // sirve wwwroot/index.html cuando se entra a "/"
app.UseStaticFiles();    // sirve css, js e imágenes de la carpeta wwwroot

// La página pide la carta acá. C# convierte los objetos a JSON automáticamente.
app.MapGet("/api/menu", () => new { categorias = MenuData.Categorias, zonas = MenuData.Zonas });

// La página manda el pedido acá. C# convierte el JSON recibido en un objeto PedidoNuevo.
app.MapPost("/api/pedidos", (PedidoNuevo pedido) =>
{
    var errores = PedidoService.Validar(pedido);
    if (errores.Count > 0)
    {
        return Results.BadRequest(new { errores });   // 400 con la lista de problemas
    }

    var creado = PedidoService.Crear(pedido);
    return Results.Ok(creado);                         // 200 con el pedido ya calculado
});

// Solo para probar mientras desarrollás: ver los pedidos guardados en memoria.
// No existe cuando la app se publica, porque muestra datos de clientes.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/pedidos", () => PedidoService.Guardados);
}

app.Run();
