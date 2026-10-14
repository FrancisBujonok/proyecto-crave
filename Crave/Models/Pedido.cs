namespace Crave.Models;

// ---------- Lo que MANDA la página (JSON -> C#) ----------

// Un producto del pedido: cuál y cuántos. El precio NO viaja: lo busca el servidor.
public class ItemPedido
{
    public string ProductoId { get; set; } = "";
    public int Cantidad { get; set; }
}

public class PedidoNuevo
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Modo { get; set; } = "delivery";          // "delivery" o "retiro"
    public string Calle { get; set; } = "";
    public string Piso { get; set; } = "";
    public string ZonaId { get; set; } = "";
    public string Referencias { get; set; } = "";
    public string Cuando { get; set; } = "ya";               // "ya" o "prog" (programado)
    public string Hora { get; set; } = "";
    public string Pago { get; set; } = "transferencia";      // "transferencia" o "efectivo"
    public int AbonaCon { get; set; }                        // con cuánto paga en efectivo (0 = exacto)
    public string Notas { get; set; } = "";
    public List<ItemPedido> Items { get; set; } = new();
}

// ---------- Lo que RESPONDE el servidor (C# -> JSON) ----------

public class LineaPedido
{
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public int Precio { get; set; }
}

public class PedidoCreado
{
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Modo { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Pago { get; set; } = "";
    public int Subtotal { get; set; }
    public int Envio { get; set; }
    public int Total { get; set; }
    public int Vuelto { get; set; }
    public List<LineaPedido> Lineas { get; set; } = new();
}
