namespace Crave.Models;

// Lo que se guarda en la base de datos cuando se confirma un pedido.
// (PedidoNuevo es lo que llega de la página y PedidoCreado es lo que se le responde; estas dos son las filas.)

// Una fila de la tabla "pedidos"
public class Pedido
{
    public long Id { get; set; }                     // lo numera la base de datos sola
    public string Numero { get; set; } = "";         // "CR-1001", el que ve el cliente
    public DateTime CreadoEn { get; set; }           // cuándo se hizo el pedido (UTC)
    public DateOnly Dia { get; set; }                // día de entrega (hora de Argentina)
    public string Hora { get; set; } = "";           // horario elegido, ej.: "21:15"
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Modo { get; set; } = "";           // "delivery" o "retiro"
    public string Direccion { get; set; } = "";
    public string? Referencias { get; set; }
    public string Pago { get; set; } = "";           // "transferencia" o "efectivo"
    public int AbonaCon { get; set; }
    public int Subtotal { get; set; }
    public int Envio { get; set; }
    public int Total { get; set; }
    public int Vuelto { get; set; }
    public string? Notas { get; set; }
    public string Estado { get; set; } = "nuevo";    // nuevo, preparando, en_camino, entregado, cancelado
    public List<PedidoItem> Items { get; set; } = new();
}

// Una fila de la tabla "pedido_items": un renglón del pedido
public class PedidoItem
{
    public long Id { get; set; }
    public long PedidoId { get; set; }               // a qué pedido pertenece
    public string? ProductoId { get; set; }
    public string Nombre { get; set; } = "";         // copia del nombre al momento de comprar
    public int Cantidad { get; set; }
    public int Precio { get; set; }                  // copia del precio al momento de comprar
}
