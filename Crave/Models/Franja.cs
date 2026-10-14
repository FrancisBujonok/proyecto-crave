namespace Crave.Models;

// Un horario de entrega que el cliente puede elegir. Ejemplo: Hora = "21:15", Libres = 3.
public class Franja
{
    public string Hora { get; set; } = "";
    public int Libres { get; set; }      // cuántos pedidos más entran en este horario (0 = completo)
}
