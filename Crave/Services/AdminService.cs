using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Lo que el panel necesita de la base de datos: ver los pedidos de hoy, cambiarles el estado y marcar productos como agotados.
// Los endpoints (AdminPanelEndpoints) solo reciben el pedido HTTP y llaman a estos métodos.
public class AdminService : IAdminService
{
    private readonly CraveDb _db;

    public AdminService(CraveDb db)
    {
        _db = db;
    }

    // Los pedidos del día de hoy (hora de Argentina), ordenados por horario de entrega.
    public async Task<List<PedidoAdmin>> PedidosDeHoy()
    {
        var hoy = DateOnly.FromDateTime(HorarioService.Ahora());

        var pedidos = await _db.Pedidos
            .Include(p => p.Items)
            .Where(p => p.Dia == hoy)
            .OrderBy(p => p.Hora)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return pedidos.Select(ConvertirPedido).ToList();
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
    static PedidoAdmin ConvertirPedido(Pedido p)
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
