using Crave.Models;

namespace Crave.Services;

// Las reglas para aceptar o rechazar lo que se escribe en el formulario de horarios.
// No toca la base de datos: solo mira los números y devuelve la lista de problemas (vacía = todo bien).
public static class ConfigReglas
{
    // Convierte "21:15" en una hora. Devuelve false si no tiene el formato HH:mm.
    public static bool TryHora(string? texto, out TimeOnly hora)
    {
        hora = default;
        return texto != null && TimeOnly.TryParseExact(texto.Trim(), "HH:mm", out hora);
    }

    public static List<string> Validar(CambioConfig cambio)
    {
        var errores = new List<string>();

        bool aperturaOk = TryHora(cambio.Apertura, out var apertura);
        bool ultimaOk = TryHora(cambio.UltimaFranja, out var ultima);

        if (!aperturaOk) errores.Add("La hora de apertura tiene que tener el formato HH:mm, por ejemplo 20:00.");
        if (!ultimaOk) errores.Add("La hora del último horario tiene que tener el formato HH:mm, por ejemplo 23:30.");
        if (aperturaOk && ultimaOk && apertura >= ultima)
            errores.Add("El último horario tiene que ser después de la apertura.");

        if (cambio.MinutosPreparacion < 0 || cambio.MinutosPreparacion > 240)
            errores.Add("El tiempo de preparación tiene que estar entre 0 y 240 minutos.");
        if (cambio.MinutosPorFranja < 5 || cambio.MinutosPorFranja > 120)
            errores.Add("Los minutos entre horarios tienen que estar entre 5 y 120.");
        if (cambio.PedidosPorFranja < 1 || cambio.PedidosPorFranja > 50)
            errores.Add("Los pedidos por horario tienen que estar entre 1 y 50.");
        if (cambio.MinutosPagoDemorado is int minutosPago && (minutosPago < 5 || minutosPago > 240))
            errores.Add("Los minutos para marcar un pago como demorado tienen que estar entre 5 y 240.");
        if ((cambio.MensajeCerrado ?? "").Trim().Length > 200)
            errores.Add("El mensaje de local cerrado puede tener hasta 200 caracteres.");

        return errores;
    }
}
