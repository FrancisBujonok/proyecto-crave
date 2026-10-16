using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Lo que el panel necesita de la base de datos: ver los pedidos de hoy, cambiarles el estado y marcar productos como agotados.
// Los endpoints (AdminPanelEndpoints) solo reciben el pedido HTTP y llaman a estos métodos.
public class AdminService : IAdminService
{
    private readonly CraveDb _db;
    private readonly IConfigService _configuracion;

    public AdminService(CraveDb db, IConfigService configuracion)
    {
        _db = db;
        _configuracion = configuracion;
    }

    // Los pedidos del día de hoy (hora de Argentina), ordenados por horario de entrega.
    // Cada uno trae su estado de pago (pendiente, demorado, confirmado...), calculado con la hora actual del servidor.
    public async Task<List<PedidoAdmin>> PedidosDeHoy()
    {
        var hoy = DateOnly.FromDateTime(HorarioService.Ahora());
        var config = await _configuracion.Obtener();
        var ahoraUtc = DateTime.UtcNow;

        var pedidos = await _db.Pedidos
            .Include(p => p.Items)
            .Where(p => p.Dia == hoy)
            .OrderBy(p => p.Hora)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return pedidos.Select(p => ConvertirPedido(p, ahoraUtc, config.MinutosPagoDemorado)).ToList();
    }

    // Cambia el estado de un pedido. Cancelar libera el lugar del horario solo,
    // porque HorarioService no cuenta los pedidos cancelados.
    public async Task<ResultadoAdmin> CambiarEstado(long id, string? estado)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null)
            return ResultadoAdmin.NoExiste("Ese pedido no existe.");

        string nuevo = (estado ?? "").Trim().ToLowerInvariant();
        string? motivo = EstadoPedido.MotivoNoPermitido(pedido.Estado, nuevo);
        if (motivo != null)
            return ResultadoAdmin.NoPermitido(motivo);

        if (pedido.Estado != nuevo)
        {
            pedido.Estado = nuevo;
            await _db.SaveChangesAsync();
        }
        return ResultadoAdmin.Hecho();
    }

    // Marca (o desmarca) que la transferencia de un pedido ya llegó. Solo vale para pedidos por transferencia
    // que no estén cancelados (las reglas están en EstadoPago). Repetir el mismo cambio no hace nada.
    public async Task<ResultadoAdmin> CambiarPago(long id, bool confirmado)
    {
        var pedido = await _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null)
            return ResultadoAdmin.NoExiste("Ese pedido no existe.");

        string? motivo = EstadoPago.MotivoNoPermitido(pedido);
        if (motivo != null)
            return ResultadoAdmin.NoPermitido(motivo);

        bool yaEstabaConfirmado = pedido.PagoConfirmadoEn != null;
        if (yaEstabaConfirmado != confirmado)
        {
            pedido.PagoConfirmadoEn = confirmado ? DateTime.UtcNow : null;
            await _db.SaveChangesAsync();
        }
        return ResultadoAdmin.Hecho();
    }

    // Todos los productos con su categoría, en el mismo orden que la carta.
    public async Task<List<ProductoAdmin>> Productos()
    {
        var categorias = await _db.Categorias
            .Include(c => c.Productos)
            .OrderBy(c => c.Orden)
            .ToListAsync();

        var lista = new List<ProductoAdmin>();
        foreach (var categoria in categorias)
        {
            foreach (var producto in categoria.Productos.OrderBy(p => p.Orden))
            {
                lista.Add(new ProductoAdmin
                {
                    Id = producto.Id,
                    Nombre = producto.Nombre,
                    Categoria = categoria.Nombre,
                    Precio = producto.Precio,
                    Agotado = producto.Agotado
                });
            }
        }
        return lista;
    }

    // Marca un producto como agotado (o lo vuelve a habilitar). La carta de los clientes lo refleja al recargar.
    // Si es una gaseosa, deja de ofrecerse también en los combos.
    public async Task<ResultadoAdmin> CambiarAgotado(string id, bool agotado)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id);
        if (producto == null)
            return ResultadoAdmin.NoExiste("Ese producto no existe.");

        if (producto.Agotado != agotado)
        {
            producto.Agotado = agotado;
            await _db.SaveChangesAsync();
        }
        return ResultadoAdmin.Hecho();
    }

    // Convierte la fila de la base en lo que se manda al panel
    static PedidoAdmin ConvertirPedido(Pedido p, DateTime ahoraUtc, int minutosPagoDemorado)
    {
        return new PedidoAdmin
        {
            Id = p.Id,
            Numero = p.Numero,
            CreadoEn = p.CreadoEn,
            Hora = p.Hora,
            Nombre = p.Nombre,
            Telefono = p.Telefono,
            Modo = p.Modo,
            Direccion = p.Direccion,
            Referencias = p.Referencias,
            Pago = p.Pago,
            EstadoPago = EstadoPago.De(p, ahoraUtc, minutosPagoDemorado),
            PagoConfirmadoEn = p.PagoConfirmadoEn,
            MinutosEsperandoPago = EstadoPago.MinutosEsperando(p, ahoraUtc),
            AbonaCon = p.AbonaCon,
            Subtotal = p.Subtotal,
            Envio = p.Envio,
            Total = p.Total,
            Vuelto = p.Vuelto,
            Notas = p.Notas,
            Estado = p.Estado,
            Items = p.Items.OrderBy(i => i.Id).Select(i => new ItemAdmin
            {
                Nombre = i.Nombre,
                Cantidad = i.Cantidad,
                Precio = i.Precio,
                Detalle = i.Detalle
            }).ToList()
        };
    }
}
