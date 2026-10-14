namespace Crave.Models;

// Un grupo de productos de la carta (por ejemplo "Tacos").
public class Categoria
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Nota { get; set; } = "";
    public string Icono { get; set; } = "";
    public List<Producto> Productos { get; set; } = new();
}
