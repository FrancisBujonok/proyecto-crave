using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

// Reparte los pedidos en horarios para que la cocina no se sature.
// Cada horario (franja) tiene un cupo. Cuando se llena, el cliente tiene que elegir otro.
// Los números (apertura, cupo, minutos...) ya no están acá: se editan desde el panel y viven en la tabla "configuracion".
// La cuenta de los horarios está en GeneradorFranjas; esta clase solo junta los datos de la base.
public class HorarioService
{
    private readonly CraveDb _db;
    private readonly IConfigService _configuracion;
    private Configuracion? _config;   // se lee una sola vez por pedido HTTP

    public HorarioService(CraveDb db, IConfigService configuracion)
    {
        _db = db;
        _configuracion = configuracion;
    }

    async Task<Configuracion> Config()
    {
        _config ??= await _configuracion.Obtener();
        return _config;
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

    // false cuando el dueño cerró el local desde el panel
    public async Task<bool> LocalAbierto()
    {
        return (await Config()).LocalAbierto;
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
    // Con el local cerrado la lista viene vacía.
    public async Task<List<Franja>> ObtenerFranjas()
    {
        var ahora = Ahora();
        var config = await Config();
        var ocupados = await ContarPedidosDeHoy(DateOnly.FromDateTime(ahora));
        return GeneradorFranjas.Generar(config, ahora, ocupados);
    }

    // Cuántos lugares quedan en UN horario de hoy (0 si no existe o está completo).
    public async Task<int> LugaresLibres(string hora)
    {
        var franjas = await ObtenerFranjas();
        var franja = franjas.FirstOrDefault(f => f.Hora == hora);
        return franja == null ? 0 : franja.Libres;
    }

    // Texto para mostrar cuando no hay ningún horario con lugar (o cuando el local está cerrado).
    public async Task<string> MensajeSinFranjas()
    {
        return GeneradorFranjas.MensajeSinFranjas(await Config(), Ahora());
    }
}
