using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Busca un pedido por su código de seguimiento y devuelve solo lo que el cliente puede ver.
public class SeguimientoService
{
    private readonly CraveDb _db;

    public SeguimientoService(CraveDb db)
    {
        _db = db;
    }

    // Devuelve null si el código no tiene la forma correcta o no es de ningún pedido.
    // En los dos casos se responde lo mismo, así nadie puede distinguir "inventé mal el código" de "no existe".
    public async Task<SeguimientoPedido?> Buscar(string? codigo)
    {
        string? limpio = SeguimientoReglas.Normalizar(codigo);
        if (limpio == null) return null;

        var pedido = await _db.Pedidos
            .AsNoTracking()   // solo lectura: no hace falta que Entity Framework vigile los cambios
            .FirstOrDefaultAsync(p => p.CodigoSeguimiento == limpio);
        if (pedido == null) return null;

        return new SeguimientoPedido
        {
            Numero = pedido.Numero,
            Estado = pedido.Estado,
            Hora = pedido.Hora,
            Pago = pedido.Pago,
            PagoConfirmado = pedido.PagoConfirmadoEn != null,
            Total = pedido.Total
        };
    }
}
