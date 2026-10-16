namespace Crave.Models;

// La configuración del local. Es la única fila (id = 1) de la tabla "configuracion".
// Se edita desde el panel de administración, en la pestaña "Horarios".
public class Configuracion
{
    public int Id { get; set; } = 1;
    public bool LocalAbierto { get; set; } = true;       // false = el local está cerrado y no se aceptan pedidos
    public string? MensajeCerrado { get; set; }          // lo que ve el cliente cuando está cerrado
    public string Apertura { get; set; } = "20:00";      // primer horario que se ofrece (HH:mm)
    public string UltimaFranja { get; set; } = "23:30";  // último horario de entrega que se ofrece
    public int MinutosPreparacion { get; set; } = 30;    // el primer horario disponible es "ahora + estos minutos"
    public int MinutosPorFranja { get; set; } = 15;      // un horario cada tantos minutos
    public int PedidosPorFranja { get; set; } = 4;       // cuántos pedidos aguanta la cocina por horario
}

// Lo que manda el panel al guardar el formulario de horarios
public record CambioConfig(
    string? MensajeCerrado,
    string? Apertura,
    string? UltimaFranja,
    int MinutosPreparacion,
    int MinutosPorFranja,
    int PedidosPorFranja);

// Lo que manda el panel al tocar el botón "Abrir local" / "Cerrar local"
public record CambioAbierto(bool Abierto);
