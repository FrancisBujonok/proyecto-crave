namespace Crave.Models;

// Una salsa que el cliente elige dentro de un combo (Chick-fil-A, CRAVE, Honey gold, Hot mayo).
// Cada objeto es una fila de la tabla "salsas". No se venden sueltas: por eso no son productos de la carta.
public class Salsa
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool Agotado { get; set; }    // true = deja de ofrecerse en todos los combos (se marca desde el panel)
    public int Orden { get; set; }       // orden en la ventana de elección
}
