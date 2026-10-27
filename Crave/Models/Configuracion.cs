namespace Crave.Models;

// La configuración del local. Es la única fila (id = 1) de la tabla "configuracion".
// Se edita desde el panel de administración, en la pestaña "Horarios".
public class Configuracion
{
    public int Id { get; set; } = 1;
    public bool LocalAbierto { get; set; } = true;       // false = el local está cerrado y no se aceptan pedidos
    public string? MensajeCerrado { get; set; }          // lo que ve el cliente cuando está cerrado
    public string Apertura { get; set; } = "20:00";      // primer horario que se ofrece (HH:mm)
    public string UltimaFranja { get; set; } = "22:30";  // último horario de entrega que se ofrece
    public int MinutosPreparacion { get; set; } = 30;    // el primer horario disponible es "ahora + estos minutos"
    public int MinutosPorFranja { get; set; } = 30;      // un horario cada tantos minutos
    public int PedidosPorFranja { get; set; } = 5;       // cuántos pedidos aguanta la cocina por horario (6 horarios x 5 = 30 por día)
    public int MinutosPagoDemorado { get; set; } = 20;   // una transferencia sin confirmar pasado este tiempo se marca "Pago demorado" en el panel
    public bool PromoActiva { get; set; }                // true = se cobran los precios de lanzamiento (columna precio_promo de cada producto)
}

// Lo que manda el panel al guardar el formulario de horarios
public record CambioConfig(
    string? MensajeCerrado,
    string? Apertura,
    string? UltimaFranja,
    int MinutosPreparacion,
    int MinutosPorFranja,
    int PedidosPorFranja,
    int? MinutosPagoDemorado = null);   // si no viene (un panel viejo que quedó abierto), se deja el valor que ya estaba

// Lo que manda el panel al tocar el botón "Abrir local" / "Cerrar local"
public record CambioAbierto(bool Abierto);

// Lo que manda el panel al tocar "Activar precios de lanzamiento" / "Volver a precios normales"
public record CambioPromo(bool Activa);
