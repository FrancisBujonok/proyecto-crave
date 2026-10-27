using Crave.Models;

namespace Crave.Services;

// Leer y guardar la configuración del local. Los demás servicios y los endpoints dependen de esta lista.
public interface IConfigService
{
    Task<Configuracion> Obtener();
    Task<List<string>> Guardar(CambioConfig cambio);   // devuelve los errores; lista vacía = se guardó
    Task CambiarAbierto(bool abierto);
    Task CambiarPromo(bool activa);   // prende o apaga los precios de lanzamiento
}
