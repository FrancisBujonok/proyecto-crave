namespace Crave.Models;

// Lo que ve el CLIENTE cuando sigue su pedido (C# -> JSON). Es público: cualquiera que tenga el código lo puede pedir,
// por eso solo lleva lo mínimo. Nunca va el nombre, el teléfono, la dirección, las referencias ni las aclaraciones.
public class SeguimientoPedido
{
    public string Numero { get; set; } = "";          // "CR-1001"
    public string Estado { get; set; } = "";          // nuevo, preparando, listo, entregado o cancelado
    public string Hora { get; set; } = "";            // horario de entrega elegido, ej.: "21:15"
    public string Modo { get; set; } = "";            // "delivery" o "retiro"
    public string Pago { get; set; } = "";            // "transferencia" o "efectivo"
    public bool PagoConfirmado { get; set; }          // true si el local ya vio llegar la transferencia
    public int Total { get; set; }
}
