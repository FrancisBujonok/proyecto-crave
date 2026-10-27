namespace Crave.Models;

// Formas de los datos que se mandan y reciben en el panel de administración.
// Son clases simples, solo para armar el JSON (no son tablas).

// ---------- Lo que manda el panel ----------
public record CambioEstado(string? Estado);
public record CambioAgotado(bool Agotado);
public record CambioPago(bool Confirmado);   // true = "transferencia recibida", false = deshacerlo

// ---------- Lo que se le responde al panel ----------
public class PedidoAdmin
{
    public long Id { get; set; }
    public string Numero { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public string Hora { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string? Referencias { get; set; }
    public string Pago { get; set; } = "";
    public string EstadoPago { get; set; } = "";             // efectivo, pendiente, demorado, confirmado o sin_pago (ver EstadoPago.cs)
    public DateTime? PagoConfirmadoEn { get; set; }          // cuándo se marcó "transferencia recibida" (null = sin confirmar)
    public int MinutosEsperandoPago { get; set; }            // minutos desde que se hizo el pedido (el panel lo muestra mientras no se confirma)
    public int AbonaCon { get; set; }
    public int Subtotal { get; set; }
    public int Envio { get; set; }
    public int Total { get; set; }
    public int Vuelto { get; set; }
    public string? Notas { get; set; }
    public string Estado { get; set; } = "";
    public List<ItemAdmin> Items { get; set; } = new();
}

public class ItemAdmin
{
    public string Nombre { get; set; } = "";
    public int Cantidad { get; set; }
    public int Precio { get; set; }
    public string? Detalle { get; set; }
}

public class ProductoAdmin
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public int Precio { get; set; }            // precio normal
    public int? PrecioPromo { get; set; }      // precio de lanzamiento (null = no tiene)
    public bool Agotado { get; set; }
    public bool Proximamente { get; set; }
}

// Resultado de un cambio: Ok = salió bien. Si no, Codigo es el número HTTP (404, 409...) y Mensaje explica por qué.
public record ResultadoAdmin(bool Ok, int Codigo, string? Mensaje)
{
    public static ResultadoAdmin Hecho() => new(true, 200, null);
    public static ResultadoAdmin NoExiste(string mensaje) => new(false, 404, mensaje);
    public static ResultadoAdmin NoPermitido(string mensaje) => new(false, 409, mensaje);
}
