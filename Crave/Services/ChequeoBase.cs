using Crave.Data;
using Microsoft.EntityFrameworkCore;

namespace Crave.Services;

public class ChequeoBase : IChequeoBase
{
    private readonly CraveDb _db;

    public ChequeoBase(CraveDb db)
    {
        _db = db;
    }

    // Lee una tabla de verdad (no solo abre la conexión): así Supabase lo cuenta como actividad del proyecto.
    public async Task Chequear(CancellationToken cancelacion)
    {
        await _db.Categorias.AsNoTracking().AnyAsync(cancelacion);
    }
}
