namespace Crave.Models;

// Un producto de la carta (por ejemplo "Combo 4 piezas"). Cada objeto es una fila de la tabla "productos".
// Cada propiedad se convierte en un campo del JSON que recibe la página.
public class Producto
{
    public string Id { get; set; } = "";
    public string CategoriaId { get; set; } = "";   // a qué categoría pertenece (clave foránea)
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Precio { get; set; }                 // en pesos, sin decimales
    public string? Icono { get; set; }              // dibujo provisorio hasta tener fotos
    public string? Imagen { get; set; }             // ruta de la foto dentro de wwwroot, ej.: "img/combo-4.png"
    public string? Etiqueta { get; set; }           // cartelito sobre la tarjeta, ej.: "Para 2"
    public bool Agotado { get; set; }
    public bool Destacado { get; set; }             // true = aparece también en la fila de Combos
    public int Orden { get; set; }                  // para ordenar dentro de la categoría

    // Cuántas gaseosas elige el cliente al pedir este producto. 0 = ninguna (la mayoría de los productos).
    // Ejemplo: "Familiar para 4" tiene 4, el menú niños tiene 1. Las opciones salen de la categoría "bebidas".
    public int GaseosasAElegir { get; set; }
}
