namespace Crave.Models;

// Un barrio de entrega con su costo de envío. Fila de la tabla "zonas" (ver sql/09-envios-y-horarios.sql).
public class Zona
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Costo { get; set; }   // en pesos
}
