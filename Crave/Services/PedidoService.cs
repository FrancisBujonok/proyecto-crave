using Crave.Data;
using Crave.Models;

namespace Crave.Services;

// Acá vive la lógica del pedido. La regla es simple: el navegador pide, el servidor decide.
// Los precios se buscan en MenuData y los totales se calculan siempre en C#,
// así nadie puede cambiar un precio desde el navegador.
public static class PedidoService
{
    // Pedidos guardados en memoria. Se pierden al apagar el servidor.
    // Cuando armemos la base de datos, esto se reemplaza por una tabla Pedidos.
    public static List<PedidoCreado> Guardados = new();

    public static int CalcularSubtotal(PedidoNuevo pedido)
    {
        int subtotal = 0;
        foreach (var item in pedido.Items)
        {
            var producto = MenuData.BuscarProducto(item.ProductoId);
            if (producto != null)
            {
                subtotal += producto.Precio * item.Cantidad;
            }
        }
        return subtotal;
    }

    public static int CalcularEnvio(PedidoNuevo pedido)
    {
        if (pedido.Modo != "delivery") return 0;

        var zona = MenuData.BuscarZona(pedido.ZonaId);
        if (zona == null) return 0;

        return zona.Costo;
    }

    // Devuelve la lista de problemas. Si la lista está vacía, el pedido es válido.
    public static List<string> Validar(PedidoNuevo pedido)
    {
        var errores = new List<string>();

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
            var producto = MenuData.BuscarProducto(item.ProductoId);

            if (producto == null)
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

            if (MenuData.BuscarZona(pedido.ZonaId) == null)
                errores.Add("Elegí la zona de entrega.");
        }

        if (pedido.Cuando == "prog" && pedido.Hora.Trim() == "")
            errores.Add("Elegí a qué hora lo querés.");

        int total = CalcularSubtotal(pedido) + CalcularEnvio(pedido);
        if (pedido.Pago == "efectivo" && pedido.AbonaCon > 0 && pedido.AbonaCon < total)
            errores.Add("Con lo que abonás no alcanza para cubrir el total.");

        return errores;
    }

    // Arma el pedido final. Solo se llama después de Validar (sin errores).
    public static PedidoCreado Crear(PedidoNuevo pedido)
    {
        var lineas = new List<LineaPedido>();
        foreach (var item in pedido.Items)
        {
            var producto = MenuData.BuscarProducto(item.ProductoId)!;   // ya sabemos que existe
            lineas.Add(new LineaPedido
            {
                Nombre = producto.Nombre,
                Cantidad = item.Cantidad,
                Precio = producto.Precio
            });
        }

        int subtotal = CalcularSubtotal(pedido);
        int envio = CalcularEnvio(pedido);
        int total = subtotal + envio;

        string direccion = "Retiro en el local";
        if (pedido.Modo == "delivery")
        {
            var zona = MenuData.BuscarZona(pedido.ZonaId)!;
            direccion = pedido.Calle.Trim();
            if (pedido.Piso.Trim() != "") direccion += " (" + pedido.Piso.Trim() + ")";
            direccion += ", " + zona.Nombre;
        }

        int vuelto = 0;
        if (pedido.Pago == "efectivo" && pedido.AbonaCon > 0)
            vuelto = pedido.AbonaCon - total;

        var creado = new PedidoCreado
        {
            Numero = "CR-" + Random.Shared.Next(1000, 10000),
            Fecha = DateTime.Now,
            Nombre = pedido.Nombre.Trim(),
            Telefono = pedido.Telefono.Trim(),
            Modo = pedido.Modo,
            Direccion = direccion,
            Pago = pedido.Pago,
            Subtotal = subtotal,
            Envio = envio,
            Total = total,
            Vuelto = vuelto,
            Lineas = lineas
        };

        lock (Guardados)   // evita problemas si entran dos pedidos al mismo tiempo
        {
            Guardados.Add(creado);
        }
        return creado;
    }
}
