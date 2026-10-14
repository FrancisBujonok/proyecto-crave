namespace Crave.Models;

// Un producto de la carta (por ejemplo "Combo 4 piezas").
// Cada propiedad se convierte en un campo del JSON que recibe la página.
public class Producto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Precio { get; set; }              // en pesos, sin decimales
    public string Icono { get; set; } = "";      // dibujo provisorio hasta tener fotos
    public string Imagen { get; set; } = "";     // ruta de la foto dentro de wwwroot, ej.: "img/combo-4.png"
    public string Etiqueta { get; set; } = "";   // cartelito sobre la tarjeta, ej.: "Para 2"
    public bool Agotado { get; set; }
    public bool Destacado { get; set; }          // true = aparece también en la fila de Combos
}
