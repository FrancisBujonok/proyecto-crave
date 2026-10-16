namespace Crave.Models;

// Cómo está el pago de un pedido y las reglas para confirmarlo.
// Solo las transferencias necesitan confirmación: el efectivo se cobra al entregar.
// El estado no se guarda en la base: se calcula cada vez a partir de lo que sí se guarda
// (el medio de pago, "pago_confirmado_en" y cuánto hace que se hizo el pedido).
public static class EstadoPago
{
    public const string Efectivo = "efectivo";       // se cobra al entregar: no hay nada que confirmar
    public const string Pendiente = "pendiente";     // transferencia que todavía no se marcó como recibida
    public const string Demorado = "demorado";       // pendiente desde hace más de N minutos (N se cambia en el panel)
    public const string Confirmado = "confirmado";   // el dueño marcó "transferencia recibida"
    public const string SinPago = "sin_pago";        // transferencia cancelada sin que llegara la plata

    const string PagoTransferencia = "transferencia";

    // El estado de pago de un pedido en este momento. "ahoraUtc" se recibe de afuera (en vez de leer el reloj acá)
    // para que la cuenta sea siempre la misma con los mismos datos.
    public static string De(Pedido pedido, DateTime ahoraUtc, int minutosDemorado)
    {
        if (pedido.Pago != PagoTransferencia) return Efectivo;
        if (pedido.PagoConfirmadoEn != null) return Confirmado;
        if (pedido.Estado == EstadoPedido.Cancelado) return SinPago;
        return MinutosEsperando(pedido, ahoraUtc) >= minutosDemorado ? Demorado : Pendiente;
    }

    // Minutos enteros desde que se hizo el pedido (nunca negativo, aunque el reloj del servidor se atrase un poco).
    public static int MinutosEsperando(Pedido pedido, DateTime ahoraUtc)
    {
        return Math.Max(0, (int)(ahoraUtc - pedido.CreadoEn).TotalMinutes);
    }

    // Si se puede marcar o desmarcar el pago de este pedido devuelve null. Si no, devuelve el motivo para mostrarlo.
    // Marcar algo que ya está marcado (o desmarcar algo que no lo está) no es un problema: no cambia nada.
    public static string? MotivoNoPermitido(Pedido pedido)
    {
        if (pedido.Pago != PagoTransferencia)
            return "Este pedido es en efectivo: se cobra al entregar, no hay transferencia que confirmar.";

        if (pedido.Estado == EstadoPedido.Cancelado)
            return "Este pedido está cancelado: ya no se puede cambiar su pago.";

        return null;
    }
}
