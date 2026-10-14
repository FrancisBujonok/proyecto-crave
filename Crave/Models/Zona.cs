namespace Crave.Models;

// Una zona de entrega con su costo de envío.
public class Zona
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Costo { get; set; }   // en pesos
}
