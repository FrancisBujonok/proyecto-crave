using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Acá vive la lógica del pedido. La regla es simple: el navegador pide, el servidor decide.
// Los precios se buscan siempre en la base de datos y los totales se calculan en C#,
// así nadie puede cambiar un precio desde el navegador.
// Qué precio se cobra (normal o de lanzamiento) lo decide Precios.Cobrar, según el interruptor del panel.
public class PedidoService
{
    private readonly CraveDb _db;
    private readonly HorarioService _horarios;
    private readonly IConfigService _config;

    public PedidoService(CraveDb db, HorarioService horarios, IConfigService config)
    {
        _db = db;
        _horarios = horarios;
        _config = config;
    }

    // ¿Están prendidos los precios de lanzamiento? Se pregunta en cada pedido, así el cambio en el panel rige enseguida.
    async Task<bool> PromoActiva()
    {
        return (await _config.Obtener()).PromoActiva;
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

    // Las gaseosas que se pueden elegir en un combo son los productos de esta categoría.
    // Así, si en el panel se marca una gaseosa como agotada, deja de ofrecerse en todos los combos.
    const string CategoriaBebidas = "bebidas";

    // Trae las gaseosas de la base. Devuelve un diccionario: id -> producto.
    async Task<Dictionary<string, Producto>> BuscarGaseosas()
    {
        return await _db.Productos.Where(p => p.CategoriaId == CategoriaBebidas).ToDictionaryAsync(p => p.Id);
    }

    // Revisa las gaseosas elegidas en un renglón. Un producto con "gaseosas a elegir" necesita
    // exactamente GaseosasAElegir x Cantidad gaseosas, y cada una tiene que existir y no estar agotada.
    static void ValidarGaseosas(ItemPedido item, Producto producto, Dictionary<string, Producto> gaseosas, List<string> errores)
    {
        var elegidas = item.Gaseosas ?? new List<string>();   // si el JSON trae null, lo tratamos como lista vacía

        if (producto.GaseosasAElegir == 0)
        {
            if (elegidas.Count > 0)
                errores.Add(producto.Nombre + " no lleva gaseosa a elección.");
            return;
        }

        if (elegidas.Count != producto.GaseosasAElegir * item.Cantidad)
        {
            errores.Add("Elegí las gaseosas de " + producto.Nombre + ".");
            return;
        }

        foreach (var id in elegidas)
        {
            if (id == null || !gaseosas.TryGetValue(id, out var gaseosa))
                errores.Add("Hay una gaseosa que ya no está en la carta.");
            else if (gaseosa.Agotado)
                errores.Add(gaseosa.Nombre + " está agotada.");
        }
    }

    // Convierte las gaseosas elegidas en el texto que se guarda y se muestra.
    // Ejemplo: [coca, coca, sprite] -> "2 × Coca-Cola, 1 × Sprite". Con una sola gaseosa: solo el nombre.
    static string? ArmarDetalle(List<string>? elegidas, Dictionary<string, Producto> gaseosas)
    {
        if (elegidas == null || elegidas.Count == 0) return null;
        if (elegidas.Count == 1) return gaseosas[elegidas[0]].Nombre;

        // GroupBy junta las repetidas y mantiene el orden en que aparecen
        var partes = elegidas
            .GroupBy(id => id)
            .Select(grupo => grupo.Count() + " × " + gaseosas[grupo.Key].Nombre);
        return string.Join(", ", partes);
    }

    static int CalcularSubtotal(PedidoNuevo pedido, Dictionary<string, Producto> productos, bool promoActiva)
    {
        int subtotal = 0;
        foreach (var item in pedido.Items)
        {
            if (productos.TryGetValue(item.ProductoId, out var producto))
            {
                subtotal += Precios.Cobrar(producto, promoActiva) * item.Cantidad;
            }
        }
        return subtotal;
    }

    // Devuelve la lista de problemas. Si la lista está vacía, el pedido es válido.
    public async Task<List<string>> Validar(PedidoNuevo pedido)
    {
        // Primero la forma (datos que faltan, textos enormes, caracteres raros): si algo está mal ni consultamos la base.
        var errores = PedidoReglas.RevisarForma(pedido);
        if (errores.Count > 0) return errores;

        var productos = await BuscarProductos(pedido);
        var gaseosas = await BuscarGaseosas();
        var zona = await BuscarZona(pedido);
        bool abierto = await _horarios.LocalAbierto();
        bool promoActiva = await PromoActiva();

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
            bool cantidadValida = item.Cantidad >= 1 && item.Cantidad <= 30;
            if (!cantidadValida)
                errores.Add("La cantidad de cada producto tiene que estar entre 1 y 30.");

            if (!productos.TryGetValue(item.ProductoId, out var producto))
            {
                errores.Add("Hay un producto que ya no está en la carta.");
                continue;   // sin producto no hay nada más que revisar en este renglón
            }

            if (producto.Proximamente)
                errores.Add(producto.Nombre + " todavía no está a la venta.");
            else if (producto.Agotado)
                errores.Add(producto.Nombre + " está agotado.");

            // Las gaseosas solo se revisan si la cantidad es razonable (evita cuentas con números enormes)
            if (cantidadValida)
                ValidarGaseosas(item, producto, gaseosas, errores);
        }

        // Un mismo producto puede llegar en varios renglones (ej.: dos combos con gaseosas distintas).
        // Sumamos todos los renglones para respetar el máximo de 30 por producto.
        bool pasaElMaximo = pedido.Items
            .GroupBy(i => i.ProductoId)
            .Any(grupo => grupo.Sum(i => (long)i.Cantidad) > 30);
        if (pasaElMaximo)
            errores.Add("La cantidad de cada producto tiene que estar entre 1 y 30.");

        if (pedido.Modo == "delivery")
        {
            if (pedido.Calle.Trim().Length < 4)
                errores.Add("Escribí la calle y el número.");

            if (zona == null)
                errores.Add("Elegí la zona de entrega.");
        }

        // Con el local cerrado no se acepta nada. Si no, el horario tiene que existir hoy y tener lugar.
        if (!abierto)
            errores.Add("El local está cerrado por el momento. Probá más tarde.");
        else if (pedido.Hora.Trim() == "")
            errores.Add("Elegí en qué horario lo querés.");
        else if (await _horarios.LugaresLibres(pedido.Hora) <= 0)
            errores.Add("Ese horario ya no está disponible. Elegí otro.");

        // Antispam: un mismo teléfono no puede tener más de 3 pedidos en curso a la vez.
        // Se compara por los últimos 8 números, así que "3493 41-2345" y "+54 9 3493 412345" cuentan como el mismo.
        if (pedido.Telefono.Count(char.IsDigit) >= 8)
        {
            var hoy = DateOnly.FromDateTime(HorarioService.Ahora());
            var telefonosEnCurso = await _db.Pedidos
                .Where(p => p.Dia == hoy && p.Estado != "cancelado" && p.Estado != "entregado")
                .Select(p => p.Telefono)
                .ToListAsync();
            string clave = PedidoReglas.ClaveTelefono(pedido.Telefono);
            if (telefonosEnCurso.Count(t => PedidoReglas.ClaveTelefono(t) == clave) >= PedidoReglas.MaxPedidosActivosPorTelefono)
                errores.Add("Ya tenés " + PedidoReglas.MaxPedidosActivosPorTelefono + " pedidos en curso con ese teléfono. Esperá a recibirlos o escribinos por WhatsApp.");
        }

        int total = CalcularSubtotal(pedido, productos, promoActiva) + (zona?.Costo ?? 0);
        if (pedido.Pago == "efectivo" && pedido.AbonaCon > 0 && pedido.AbonaCon < total)
            errores.Add("Con lo que abonás no alcanza para cubrir el total.");

        return errores.Distinct().ToList();   // Distinct saca los mensajes repetidos (ej.: dos renglones con el mismo problema)
    }

    // Arma el pedido final y lo guarda. Solo se llama después de Validar (sin errores).
    // Devuelve null si el horario se llenó justo mientras tanto (dos clientes eligiendo el último lugar).
    public async Task<PedidoCreado?> Crear(PedidoNuevo pedido)
    {
        var productos = await BuscarProductos(pedido);
        var gaseosas = await BuscarGaseosas();
        var zona = await BuscarZona(pedido);
        bool promoActiva = await PromoActiva();

        int subtotal = CalcularSubtotal(pedido, productos, promoActiva);
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
            Notas = pedido.Notas.Trim() == "" ? null : pedido.Notas.Trim(),
            CodigoSeguimiento = SeguimientoReglas.NuevoCodigo()   // con este código el cliente sigue su pedido
        };

        foreach (var item in pedido.Items)
        {
            var producto = productos[item.ProductoId];   // ya sabemos que existe
            nuevo.Items.Add(new PedidoItem
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                Cantidad = item.Cantidad,
                Precio = Precios.Cobrar(producto, promoActiva),   // se guarda el precio que se cobró (con o sin promo)
                Detalle = ArmarDetalle(item.Gaseosas, gaseosas)   // null si el producto no lleva gaseosas a elegir
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
            Codigo = nuevo.CodigoSeguimiento ?? "",
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
            Lineas = nuevo.Items.Select(i => new LineaPedido { Nombre = i.Nombre, Cantidad = i.Cantidad, Precio = i.Precio, Detalle = i.Detalle }).ToList()
        };
    }
}
