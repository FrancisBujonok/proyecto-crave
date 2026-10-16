using Crave.Models;

namespace Crave.Services;

// Arma la lista de horarios del día a partir de la configuración. No usa la base de datos:
// recibe la configuración, la hora actual y cuántos pedidos hay en cada horario, y devuelve el resultado.
public static class GeneradorFranjas
{
    // "20:00" -> las 20:00 de hoy. Devuelve false si el texto no es una hora válida.
    static bool TryHoraDeHoy(string texto, DateTime ahora, out DateTime hora)
    {
        hora = default;
        if (!ConfigReglas.TryHora(texto, out var solo)) return false;
        hora = ahora.Date.AddHours(solo.Hour).AddMinutes(solo.Minute);
        return true;
    }

    // ocupados: cuántos pedidos de hoy eligieron cada horario. Ejemplo: { "21:15": 3, "21:30": 1 }
    // Si el local está cerrado no hay ningún horario.
    public static List<Franja> Generar(Configuracion config, DateTime ahora, Dictionary<string, int> ocupados)
    {
        var franjas = new List<Franja>();
        if (!config.LocalAbierto) return franjas;
        if (!TryHoraDeHoy(config.Apertura, ahora, out var primera)) return franjas;
        if (!TryHoraDeHoy(config.UltimaFranja, ahora, out var ultima)) return franjas;

        int paso = Math.Max(config.MinutosPorFranja, 1);          // evita un bucle infinito si alguien guardó 0
        var minimo = ahora.AddMinutes(config.MinutosPreparacion); // nada antes de esta hora

        for (var hora = primera; hora <= ultima; hora = hora.AddMinutes(paso))
        {
            if (hora < minimo) continue;   // ya no llegamos con ese horario

            string texto = hora.ToString("HH:mm");
            ocupados.TryGetValue(texto, out int usados);   // si nadie lo eligió, usados queda en 0

            franjas.Add(new Franja { Hora = texto, Libres = Math.Max(config.PedidosPorFranja - usados, 0) });
        }
        return franjas;
    }

    // Texto para mostrar cuando el cliente no puede elegir ningún horario.
    public static string MensajeSinFranjas(Configuracion config, DateTime ahora)
    {
        if (!config.LocalAbierto)
        {
            return string.IsNullOrWhiteSpace(config.MensajeCerrado)
                ? "Por el momento no estamos tomando pedidos."
                : config.MensajeCerrado.Trim();
        }

        if (TryHoraDeHoy(config.UltimaFranja, ahora, out var ultima) && ahora.AddMinutes(config.MinutosPreparacion) > ultima)
            return "Por hoy ya no tomamos más pedidos. Atendemos de " + config.Apertura + " a " + config.UltimaFranja + ".";

        return "Se completaron todos los horarios de hoy. Probá de nuevo más tarde.";
    }
}
