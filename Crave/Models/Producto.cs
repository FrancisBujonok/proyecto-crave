namespace Crave.Models;

// Un producto de la carta (por ejemplo "Combo 4 piezas"). Cada objeto es una fila de la tabla "productos".
// Cada propiedad se convierte en un campo del JSON que recibe la página.
public class Producto
{
    public string Id { get; set; } = "";
    public string CategoriaId { get; set; } = "";   // a qué categoría pertenece (clave foránea)
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Precio { get; set; }                 // precio normal, en pesos, sin decimales

    // Precio de lanzamiento (o de cualquier promo). null = este producto no tiene precio de promo.
    // Solo se cobra cuando la promo está prendida desde el panel (Configuracion.PromoActiva). La regla está en Services/Precios.cs.
    public int? PrecioPromo { get; set; }
    public string? Icono { get; set; }              // dibujo provisorio hasta tener fotos
    public string? Imagen { get; set; }             // ruta de la foto dentro de wwwroot, ej.: "img/combo-4.png"
    public string? Etiqueta { get; set; }           // cartelito sobre la tarjeta, ej.: "Para 2"
    public bool Agotado { get; set; }
    public bool Proximamente { get; set; }          // true = se ve con la franja "Próximamente" y todavía no se puede pedir (ej.: los tacos)
    public bool Destacado { get; set; }             // true = aparece también en la fila de Combos
    public int Orden { get; set; }                  // para ordenar dentro de la categoría

    // Cuántas gaseosas elige el cliente al pedir este producto. 0 = ninguna (la mayoría de los productos).
    // Ejemplo: "Familiar para 4" tiene 4, el menú niños tiene 1. Las opciones salen de la categoría "bebidas".
    public int GaseosasAElegir { get; set; }

    // Cuántas salsas elige el cliente (una por persona, como las gaseosas). 0 = ninguna. Las opciones salen de la tabla "salsas".
    public int SalsasAElegir { get; set; }
}
