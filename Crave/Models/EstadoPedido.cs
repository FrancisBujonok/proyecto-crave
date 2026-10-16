namespace Crave.Models;

// Los estados por los que pasa un pedido y las reglas para cambiar de uno a otro.
// El flujo normal es: nuevo -> preparando -> listo -> entregado. "cancelado" se puede usar antes de entregar.
public static class EstadoPedido
{
    public const string Nuevo = "nuevo";
    public const string Preparando = "preparando";
    public const string Listo = "listo";
    public const string Entregado = "entregado";
    public const string Cancelado = "cancelado";

    public static readonly string[] Todos = { Nuevo, Preparando, Listo, Entregado, Cancelado };

    // Si el cambio de "actual" a "nuevo" está permitido devuelve null. Si no, devuelve el motivo para mostrarlo.
    // Se permite volver atrás (por si el dueño toca el botón equivocado), salvo con los cancelados:
    // un pedido cancelado ya liberó su lugar en el horario, y reactivarlo podría pasarse del cupo.
    public static string? MotivoNoPermitido(string actual, string nuevo)
    {
        if (!Todos.Contains(nuevo))
            return "Estado inválido.";

        if (actual == nuevo)
            return null;   // no cambia nada, no es un problema

        if (actual == Cancelado)
            return "Un pedido cancelado no se puede reactivar. Que el cliente haga uno nuevo.";

        if (actual == Entregado && nuevo == Cancelado)
            return "Un pedido entregado no se puede cancelar.";

        return null;
    }
}
