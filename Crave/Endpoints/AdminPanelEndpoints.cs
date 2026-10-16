using Crave.Models;
using Crave.Services;

namespace Crave.Endpoints;

// Las direcciones del panel que trabajan con pedidos y productos.
// Se cuelgan del grupo "/api/admin" de AdminEndpoints, que ya exige tener la sesión iniciada.
public static class AdminPanelEndpoints
{
    public static void MapearPanel(this RouteGroupBuilder admin)
    {
        // Pedidos de hoy con sus renglones
        admin.MapGet("/pedidos", async (IAdminService servicio) =>
            Results.Ok(await servicio.PedidosDeHoy()));

        // Cambiar el estado de un pedido. Body: { "estado": "preparando" }
        admin.MapPatch("/pedidos/{id:long}/estado", async (long id, CambioEstado cambio, IAdminService servicio) =>
            Responder(await servicio.CambiarEstado(id, cambio.Estado)));

        // Configuración del local: abierto/cerrado, horarios, cupo y tiempo de preparación
        admin.MapGet("/configuracion", async (IConfigService config) =>
            Results.Ok(await config.Obtener()));

        // Guardar el formulario de horarios. Si algo está mal responde 400 con la lista de errores.
        admin.MapPut("/configuracion", async (CambioConfig cambio, IConfigService config) =>
        {
            var errores = await config.Guardar(cambio);
            return errores.Count > 0 ? Results.BadRequest(new { errores }) : Results.Ok(new { ok = true });
        });

        // Abrir o cerrar el local. Body: { "abierto": false }
        admin.MapPatch("/configuracion/abierto", async (CambioAbierto cambio, IConfigService config) =>
        {
            await config.CambiarAbierto(cambio.Abierto);
            return Results.Ok(new { ok = true });
        });

        // Lista de productos para marcar agotados
        admin.MapGet("/productos", async (IAdminService servicio) =>
            Results.Ok(await servicio.Productos()));

        // Marcar o desmarcar "agotado". Body: { "agotado": true }
        admin.MapPatch("/productos/{id}/agotado", async (string id, CambioAgotado cambio, IAdminService servicio) =>
            Responder(await servicio.CambiarAgotado(id, cambio.Agotado)));
    }

    // 200 si salió bien; si no, el código que corresponde (404, 409) con el motivo
    static IResult Responder(ResultadoAdmin resultado)
    {
        if (resultado.Ok)
            return Results.Ok(new { ok = true });
        return Results.Json(new { error = resultado.Mensaje }, statusCode: resultado.Codigo);
    }
}
