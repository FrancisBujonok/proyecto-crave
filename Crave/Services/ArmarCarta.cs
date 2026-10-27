using Crave.Models;

namespace Crave.Services;

// Convierte las filas de la base (categorías, productos y zonas) en la carta que ve el cliente.
// No toca la base: recibe las listas ya cargadas, así se puede probar sola.
public static class ArmarCarta
{
    public static CartaPublica Desde(List<Categoria> categorias, List<Zona> zonas, bool promoActiva)
    {
        var lista = new List<CategoriaCarta>();
        foreach (var categoria in categorias.OrderBy(c => c.Orden))
        {
            var productos = categoria.Productos
                .OrderBy(p => p.Orden)
                .Select(p => ConvertirProducto(p, promoActiva))
                .ToList();
            lista.Add(new CategoriaCarta(categoria.Id, categoria.Nombre, categoria.Nota, categoria.Icono, categoria.Orden, productos));
        }
        return new CartaPublica(promoActiva, lista, zonas);
    }

    static ProductoCarta ConvertirProducto(Producto p, bool promoActiva)
    {
        int precio = Precios.Cobrar(p, promoActiva);
        int? antes = Precios.Tachado(p, promoActiva);
        int descuento = antes is int normal ? Precios.PorcentajeDescuento(normal, precio) : 0;

        return new ProductoCarta(
            p.Id, p.CategoriaId, p.Nombre, p.Descripcion,
            precio, antes, descuento,
            p.Icono, p.Imagen, p.Etiqueta,
            p.Agotado, p.Proximamente, p.Destacado, p.Orden, p.GaseosasAElegir);
    }
}
