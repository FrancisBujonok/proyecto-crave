using Crave.Data;
using Crave.Models;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

public class ConfigService : IConfigService
{
    private readonly CraveDb _db;

    public ConfigService(CraveDb db)
    {
        _db = db;
    }

    // La fila de configuración. Si todavía no existe (no se corrió el SQL 04), devuelve los valores de siempre
    // en memoria, así la página de los clientes sigue funcionando.
    public async Task<Configuracion> Obtener()
    {
        var fila = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Id == 1);
        return fila ?? new Configuracion();
    }

    public async Task<List<string>> Guardar(CambioConfig cambio)
    {
        var errores = ConfigReglas.Validar(cambio);
        if (errores.Count > 0) return errores;

        var fila = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Id == 1);
        if (fila == null)
        {
            fila = new Configuracion { Id = 1 };
            _db.Configuraciones.Add(fila);
        }

        string mensaje = (cambio.MensajeCerrado ?? "").Trim();
        fila.MensajeCerrado = mensaje == "" ? null : mensaje;
        fila.Apertura = cambio.Apertura!.Trim();
        fila.UltimaFranja = cambio.UltimaFranja!.Trim();
        fila.MinutosPreparacion = cambio.MinutosPreparacion;
        fila.MinutosPorFranja = cambio.MinutosPorFranja;
        fila.PedidosPorFranja = cambio.PedidosPorFranja;

        await _db.SaveChangesAsync();
        return errores;   // vacía
    }

    // Abre o cierra el local sin tocar el resto de la configuración
    public async Task CambiarAbierto(bool abierto)
    {
        var fila = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Id == 1);
        if (fila == null)
        {
            fila = new Configuracion { Id = 1 };
            _db.Configuraciones.Add(fila);
        }
        fila.LocalAbierto = abierto;
        await _db.SaveChangesAsync();
    }
}
