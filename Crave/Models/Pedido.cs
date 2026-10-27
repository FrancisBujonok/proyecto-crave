namespace Crave.Models;

// ---------- Lo que MANDA la página (JSON -> C#) ----------

// Un producto del pedido: cuál y cuántos. El precio NO viaja: lo busca el servidor.
public class ItemPedido
{
    public string ProductoId { get; set; } = "";
    public int Cantidad { get; set; }

    // Solo para productos con "gaseosas a elegir": el id de cada gaseosa elegida (se pueden repetir).
    // Ejemplo para un Familiar para 4: [ "beb-coca", "beb-coca", "beb-sprite", "beb-fanta" ]
    public List<string> Gaseosas { get; set; } = new();
}

// Todos los pedidos son con envío a domicilio: no hay retiro en el local, así que siempre hay dirección y barrio.
public class PedidoNuevo
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Calle { get; set; } = "";
    public string Piso { get; set; } = "";
    public string ZonaId { get; set; } = "";                 // el barrio (fila de la tabla "zonas"); define el costo de envío
    public string Referencias { get; set; } = "";
    public string Hora { get; set; } = "";                   // horario elegido, ej.: "21:15" (sale de /api/franjas)
    public string Pago { get; set; } = "transferencia";      // "transferencia" o "efectivo"
    public int AbonaCon { get; set; }                        // con cuánto paga en efectivo (0 = exacto)
    public string Notas { get; set; } = "";
    public List<ItemPedido> Items { get; set; } = new();

    // Antispam (ver PedidoReglas): "website" es un campo oculto que una persona nunca llena,
    // y "segundos" es el tiempo que la página estuvo abierta antes de confirmar.
    public string? Website { get; set; }
    public int? Segundos { get; set; }
}

// ---------- Lo que RESPONDE el servidor (C# -> JSON) ----------

public class LineaPedido
{
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public int Precio { get; set; }
    public string? Detalle { get; set; }   // gaseosas elegidas, para mostrar debajo del nombre
}

public class PedidoCreado
{
    public string Numero { get; set; } = "";
    public string Codigo { get; set; } = "";   // código de seguimiento: va en el enlace /seguimiento.html?c=CODIGO
    public DateTime Fecha { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Hora { get; set; } = "";
    public string Pago { get; set; } = "";
    public int Subtotal { get; set; }
    public int Envio { get; set; }
    public int Total { get; set; }
    public int Vuelto { get; set; }
    public List<LineaPedido> Lineas { get; set; } = new();
}
