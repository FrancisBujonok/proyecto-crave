namespace Crave.Services;

// Un "latido": cada cierto tiempo consulta la base de datos, aunque nadie esté usando la página.
// Supabase (plan gratuito) pausa los proyectos que pasan una semana con poca actividad: este latido lo evita.
//
// OJO: solo funciona si el servidor está despierto. En el plan gratuito de Render el servidor se duerme a los
// 15 minutos sin visitas y mientras duerme no corre nada; ahí hace falta un monitor externo que lo visite
// (ver LEEME.md). Con un plan pago de Render el servidor no se duerme y este latido alcanza solo.
public class LatidoBaseDeDatos : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LatidoBaseDeDatos> _log;
    private readonly TimeSpan _cada;

    public LatidoBaseDeDatos(IServiceScopeFactory scopes, ILogger<LatidoBaseDeDatos> log, IConfiguration configuracion)
    {
        _scopes = scopes;
        _log = log;

        // Por defecto cada 6 horas (21600 segundos). Se puede cambiar con la variable Latido__CadaSegundos.
        int segundos = configuracion.GetValue("Latido:CadaSegundos", 6 * 60 * 60);
        _cada = TimeSpan.FromSeconds(Math.Max(segundos, 1));
    }

    // Esto corre en segundo plano desde que arranca el servidor hasta que se apaga.
    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                // Cada latido usa su propio "scope" (su propia conexión a la base), igual que cada pedido HTTP
                using var scope = _scopes.CreateScope();
                var chequeo = scope.ServiceProvider.GetRequiredService<IChequeoBase>();
                await chequeo.Chequear(cancelacion);
                _log.LogInformation("Latido a la base de datos: ok");
            }
            catch (OperationCanceledException)
            {
                break;   // el servidor se está apagando
            }
            catch (Exception ex)
            {
                // Si la base falla, lo anotamos y seguimos: el próximo latido lo vuelve a intentar.
                // Una excepción sin atrapar acá apagaría el servidor entero.
                _log.LogWarning(ex, "Latido a la base de datos: falló");
            }

            try
            {
                await Task.Delay(_cada, cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
