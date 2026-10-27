namespace Crave.Models;

// Lo que recibe la página de los clientes en /api/menu.
// No se manda la fila de la base tal cual: se manda el precio que se cobra HOY (con o sin promo)
// y, si hay promo, el precio de antes para mostrarlo tachado. Así el JavaScript no decide precios.
// Los "record" son clases cortas que solo guardan datos. C# los convierte a JSON con los nombres en camelCase
// (PromoActiva -> promoActiva, PrecioAntes -> precioAntes).

public record CartaPublica(
    bool PromoActiva,                 // true = la semana de lanzamiento está prendida desde el panel
    List<CategoriaCarta> Categorias,
    List<Zona> Zonas);

public record CategoriaCarta(
    string Id,
    string Nombre,
    string? Nota,
    string? Icono,
    int Orden,
    List<ProductoCarta> Productos);

public record ProductoCarta(
    string Id,
    string CategoriaId,
    string Nombre,
    string Descripcion,
    int Precio,              // el que se cobra hoy
    int? PrecioAntes,        // el precio normal tachado; null si hoy no hay descuento
    int Descuento,           // porcentaje para la etiqueta, ej.: 18 (0 si no hay descuento)
    string? Icono,
    string? Imagen,
    string? Etiqueta,
    bool Agotado,
    bool Proximamente,
    bool Destacado,
    int Orden,
    int GaseosasAElegir);
