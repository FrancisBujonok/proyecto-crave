using Crave.Models;

namespace Crave.Services;

// Lo que el panel necesita de la base de datos. Los endpoints dependen de esta lista y no de la base directamente.
public interface IAdminService
{
    Task<List<PedidoAdmin>> PedidosDeHoy();
    Task<ResultadoAdmin> CambiarEstado(long id, string? estado);
    Task<List<ProductoAdmin>> Productos();
    Task<ResultadoAdmin> CambiarAgotado(string id, bool agotado);
}
