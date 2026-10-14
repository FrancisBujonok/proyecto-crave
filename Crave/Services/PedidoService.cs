using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Acá vive la lógica del pedido. La regla es simple: el navegador pide, el servidor decide.
// Los precios se buscan siempre en la base de datos y los totales se calculan en C#,
// así nadie puede cambiar un precio desde el navegador.
public class PedidoService
{
    private readonly CraveDb _db;
    private readonly HorarioService _horarios;

    public PedidoService(CraveDb db, HorarioService horarios)
    {
        _db = db;
        _horarios = horarios;
    }

    // Trae de la base los productos que pidió el cliente. Devuelve un diccionario: id -> producto.
    async Task<Dictionary<string, Producto>> BuscarProductos(PedidoNuevo pedido)
    {
        var ids = pedido.Items.Select(i => i.ProductoId).ToList();
        return await _db.Productos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
    }

    // Solo hay zona si el pedido es delivery y la zona existe. Si no, devuelve null.
    async Task<Zona?> BuscarZona(PedidoNuevo pedido)
    {
        if (pedido.Modo != "delivery") return null;
        return await _db.Zonas.FirstOrDefaultAsync(z => z.Id == pedido.ZonaId);
    }

    static int CalcularSubtotal(PedidoNuevo pedido, Dictionary<string, Producto> productos)
    {
        int subtotal = 0;
        foreach (var item in pedido.Items)
        {
            if (productos.TryGetValue(item.ProductoId, out var producto))
            {
                subtotal += producto.Precio * item.Cantidad;
            }
        }
        return subtotal;
    }

    // Devuelve la lista de problemas. Si la lista está vacía, el pedido es válido.
    public async Task<List<string>> Validar(PedidoNuevo pedido)
    {
        var errores = new List<string>();
        var productos = await BuscarProductos(pedido);
        var zona = await BuscarZona(pedido);

        if (pedido.Nombre.Trim().Length < 3)
            errores.Add("Escribí tu nombre y apellido.");

        if (pedido.Telefono.Count(char.IsDigit) < 8)
            errores.Add("Escribí un teléfono con característica.");

        if (pedido.Modo != "delivery" && pedido.Modo != "retiro")
            errores.Add("Elegí delivery o retiro.");

        if (pedido.Pago != "transferencia" && pedido.Pago != "efectivo")
            errores.Add("Elegí cómo pagás.");

        if (pedido.Items.Count == 0)
            errores.Add("El pedido está vacío.");

        foreach (var item in pedido.Items)
        {
            if (!productos.TryGetValue(item.ProductoId, out var producto))
                errores.Add("Hay un producto que ya no está en la carta.");
            else if (producto.Agotado)
                errores.Add(producto.Nombre + " está agotado.");

            if (item.Cantidad < 1 || item.Cantidad > 30)
                errores.Add("La cantidad de cada producto tiene que estar entre 1 y 30.");
        }

        if (pedido.Modo == "delivery")
        {
            if (pedido.Calle.Trim().Length < 4)
                errores.Add("Escribí la calle y el número.");

            if (zona == null)
                errores.Add("Elegí la zona de entrega.");
        }

        // El horario tiene que existir hoy y tener lugar.
        if (pedido.Hora.Trim() == "")
            errores.Add("Elegí en qué horario lo querés.");
        else if (await _horarios.LugaresLibres(pedido.Hora) <= 0)
            errores.Add("Ese horario ya no está disponible. Elegí otro.");

        int total = CalcularSubtotal(pedido, productos) + (zona?.Costo ?? 0);
        if (pedido.Pago == "efectivo" && pedido.AbonaCon > 0 && pedido.AbonaCon < total)
            errores.Add("Con lo que abonás no alcanza para cubrir el total.");

        return errores;
    }

    // Arma el pedido final y lo guarda. Solo se llama después de Validar (sin errores).
    // Devuelve null si el horario se llenó justo mientras tanto (dos clientes eligiendo el último lugar).
    public async Task<PedidoCreado?> Crear(PedidoNuevo pedido)
    {
        var productos = await BuscarProductos(pedido);
        var zona = await BuscarZona(pedido);

        int subtotal = CalcularSubtotal(pedido, productos);
        int envio = zona?.Costo ?? 0;
        int total = subtotal + envio;

        string direccion = "Retiro en el local";
        if (zona != null)
        {
            direccion = pedido.Calle.Trim();
            if (pedido.Piso.Trim() != "") direccion += " (" + pedido.Piso.Trim() + ")";
            direccion += ", " + zona.Nombre;
        }

        int vuelto = 0;
        if (pedido.Pago == "efectivo" && pedido.AbonaCon > 0)
            vuelto = pedido.AbonaCon - total;

        var ahora = HorarioService.Ahora();
        var nuevo = new Pedido
        {
            Numero = Guid.NewGuid().ToString(),   // provisorio: se reemplaza abajo por "CR-1001"
            CreadoEn = DateTime.UtcNow,
            Dia = DateOnly.FromDateTime(ahora),
            Hora = pedido.Hora,
            Nombre = pedido.Nombre.Trim(),
            Telefono = pedido.Telefono.Trim(),
            Modo = pedido.Modo,
            Direccion = direccion,
            Referencias = pedido.Referencias.Trim() == "" ? null : pedido.Referencias.Trim(),
            Pago = pedido.Pago,
            AbonaCon = pedido.AbonaCon,
            Subtotal = subtotal,
            Envio = envio,
            Total = total,
            Vuelto = vuelto,
            Notas = pedido.Notas.Trim() == "" ? null : pedido.Notas.Trim()
        };

        foreach (var item in pedido.Items)
        {
            var producto = productos[item.ProductoId];   // ya sabemos que existe
            nuevo.Items.Add(new PedidoItem
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                Cantidad = item.Cantidad,
                Precio = producto.Precio
            });
        }

        // Todo lo que sigue va en una transacción: o se guarda completo o no se guarda nada.
        await using var transaccion = await _db.Database.BeginTransactionAsync();

        // Candado: si dos pedidos eligen el mismo horario al mismo tiempo, el segundo espera acá
        // hasta que el primero termine. Así nunca se pasa del cupo.
        string clave = nuevo.Dia.ToString("yyyy-MM-dd") + " " + nuevo.Hora;
        await _db.Database.ExecuteSqlInterpolatedAsync($"select pg_advisory_xact_lock(hashtext({clave}))");

        // Revisamos el cupo de nuevo, ya con el candado puesto
        if (await _horarios.LugaresLibres(nuevo.Hora) <= 0) return null;

        _db.Pedidos.Add(nuevo);
        await _db.SaveChangesAsync();   // acá la base le asigna el Id al pedido

        // Número de pedido correlativo: el primero es CR-1001, el siguiente CR-1002...
        nuevo.Numero = "CR-" + (1000 + nuevo.Id);
        await _db.SaveChangesAsync();

        await transaccion.CommitAsync();

        return new PedidoCreado
        {
            Numero = nuevo.Numero,
            Fecha = ahora,
            Nombre = nuevo.Nombre,
            Telefono = nuevo.Telefono,
            Modo = nuevo.Modo,
            Direccion = direccion,
            Hora = nuevo.Hora,
            Pago = nuevo.Pago,
            Subtotal = subtotal,
            Envio = envio,
            Total = total,
            Vuelto = vuelto,
            Lineas = nuevo.Items.Select(i => new LineaPedido { Nombre = i.Nombre, Cantidad = i.Cantidad, Precio = i.Precio }).ToList()
        };
    }
}
