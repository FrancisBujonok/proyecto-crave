using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Reparte los pedidos en horarios para que la cocina no se sature.
// Cada horario (franja) tiene un cupo. Cuando se llena, el cliente tiene que elegir otro.
// Para cambiar el funcionamiento solo hay que tocar los números de acá abajo.
public class HorarioService
{
    // ---------- CONFIGURACIÓN (valores de ejemplo, ajustalos con tu amigo) ----------
    public const int MinutosPreparacion = 30;   // el primer horario disponible es "ahora + 30 minutos"
    public const int MinutosPorFranja = 15;     // un horario cada 15 minutos
    public const int PedidosPorFranja = 4;      // cuántos pedidos aguanta la cocina por franja
    public const string Apertura = "20:00";
    public const string UltimaFranja = "23:30"; // la última hora de entrega que se ofrece

    private readonly CraveDb _db;

    public HorarioService(CraveDb db)
    {
        _db = db;
    }

    // Hora actual de Argentina (así funciona igual si el servidor está en otro país).
    public static DateTime Ahora()
    {
        try
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.Now;
        }
    }

    // "20:00" -> las 20:00 de hoy
    static DateTime HoraDeHoy(string texto, DateTime ahora)
    {
        var partes = texto.Split(':');
        return ahora.Date.AddHours(int.Parse(partes[0])).AddMinutes(int.Parse(partes[1]));
    }

    // Cuántos pedidos de hoy ya eligieron cada horario. Ejemplo: { "21:15": 3, "21:30": 1 }
    // Los pedidos cancelados no cuentan: liberan el lugar.
    async Task<Dictionary<string, int>> ContarPedidosDeHoy(DateOnly hoy)
    {
        return await _db.Pedidos
            .Where(p => p.Dia == hoy && p.Estado != "cancelado")
            .GroupBy(p => p.Hora)
            .Select(grupo => new { Hora = grupo.Key, Cantidad = grupo.Count() })
            .ToDictionaryAsync(x => x.Hora, x => x.Cantidad);
    }

    // La lista que ve el cliente: solo horarios que todavía se pueden pedir (los completos salen con Libres = 0).
    public async Task<List<Franja>> ObtenerFranjas()
    {
        var ahora = Ahora();
        var ocupados = await ContarPedidosDeHoy(DateOnly.FromDateTime(ahora));
        var franjas = new List<Franja>();

        var primera = HoraDeHoy(Apertura, ahora);
        var ultima = HoraDeHoy(UltimaFranja, ahora);
        var minimo = ahora.AddMinutes(MinutosPreparacion);   // nada antes de esta hora

        for (var hora = primera; hora <= ultima; hora = hora.AddMinutes(MinutosPorFranja))
        {
            if (hora < minimo) continue;   // ya no llegamos con ese horario

            string texto = hora.ToString("HH:mm");
            ocupados.TryGetValue(texto, out int usados);   // si nadie lo eligió, usados queda en 0

            franjas.Add(new Franja { Hora = texto, Libres = Math.Max(PedidosPorFranja - usados, 0) });
        }
        return franjas;
    }

    // Cuántos lugares quedan en UN horario de hoy (0 si no existe o está completo).
    public async Task<int> LugaresLibres(string hora)
    {
        var franjas = await ObtenerFranjas();
        var franja = franjas.FirstOrDefault(f => f.Hora == hora);
        return franja == null ? 0 : franja.Libres;
    }

    // Texto para mostrar cuando no hay ningún horario con lugar.
    public static string MensajeSinFranjas()
    {
        var ahora = Ahora();
        var ultima = HoraDeHoy(UltimaFranja, ahora);

        if (ahora.AddMinutes(MinutosPreparacion) > ultima)
            return "Por hoy ya no tomamos más pedidos. Atendemos de " + Apertura + " a " + UltimaFranja + ".";

        return "Se completaron todos los horarios de hoy. Probá de nuevo más tarde.";
    }
}
