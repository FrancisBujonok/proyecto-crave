using Crave.Models;

namespace Crave.Services;

// La regla de precios de todo el sistema: qué precio se cobra por un producto.
// La usan la carta que ve el cliente (/api/menu), el cálculo del pedido (PedidoService) y nada más:
// si algún día cambia la regla, se cambia solo acá.
// No toca la base de datos, así se puede probar sola.
public static class Precios
{
    // ¿El producto tiene un precio de promo que se pueda usar? Tiene que ser mayor que 0 y menor que el normal.
    // (La base ya lo exige con la regla precio_promo_valido, pero si alguien la borra, acá igual no se rompe nada.)
    public static bool TienePromo(Producto producto)
    {
        return producto.PrecioPromo is int promo && promo > 0 && promo < producto.Precio;
    }

    // El precio que se cobra hoy: el de promo si la promo está prendida y el producto lo tiene; si no, el normal.
    public static int Cobrar(Producto producto, bool promoActiva)
    {
        if (promoActiva && TienePromo(producto))
            return producto.PrecioPromo!.Value;   // el "!" le dice a C# que acá ya sabemos que no es null
        return producto.Precio;
    }

    // El precio "de antes", el que se muestra tachado. null = no hay nada que tachar.
    public static int? Tachado(Producto producto, bool promoActiva)
    {
        if (promoActiva && TienePromo(producto))
            return producto.Precio;
        return null;
    }

    // Cuánto por ciento de descuento, redondeado para abajo: 18,5 % se muestra como 18 %.
    // Así la etiqueta nunca promete más descuento del que hay.
    public static int PorcentajeDescuento(int normal, int conDescuento)
    {
        if (normal <= 0 || conDescuento >= normal) return 0;
        long ahorro = normal - conDescuento;
        return (int)(ahorro * 100 / normal);   // la división entre enteros ya redondea para abajo
    }
}
