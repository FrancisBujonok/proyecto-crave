namespace Crave.Services;

// "Tocar" la base de datos con una consulta chica para ver que responde.
// Lo usan dos cosas: /api/salud (para que un monitor externo vea que todo funciona)
// y el latido interno (para que Supabase no pause el proyecto por inactividad).
public interface IChequeoBase
{
    Task Chequear(CancellationToken cancelacion);   // lanza una excepción si la base no responde
}
