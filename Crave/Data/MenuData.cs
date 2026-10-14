using Crave.Models;

namespace Crave.Data;

// La carta del local. Para cambiar un precio, agregar un producto o marcarlo como agotado,
// editá este archivo y volvé a ejecutar el proyecto. Más adelante estos datos van a vivir
// en la base de datos (tablas Categorias, Productos y Zonas).
public static class MenuData
{
    public static List<Categoria> Categorias = new()
    {
        new Categoria
        {
            Id = "pollo", Nombre = "Pollo frito", Nota = "Combos y baldes", Icono = "pollo",
            Productos = new List<Producto>
            {
                new Producto { Id = "p2", Nombre = "Combo 2 piezas", Descripcion = "2 piezas de pollo frito, papas y una salsa.", Precio = 7900, Icono = "pollo", Etiqueta = "Para 1", Destacado = true },
                new Producto { Id = "p4", Nombre = "Combo 4 piezas", Descripcion = "4 piezas de pollo frito, papas y dos salsas.", Precio = 13900, Icono = "pollo", Etiqueta = "Para 2", Destacado = true },
                new Producto { Id = "p8", Nombre = "Balde 8 piezas", Descripcion = "8 piezas para compartir, papas grandes y tres salsas.", Precio = 24500, Icono = "balde", Etiqueta = "Para 4", Destacado = true },
                new Producto { Id = "tend", Nombre = "Tenders x6", Descripcion = "6 tiras de pechuga crocante con salsa a elección.", Precio = 8900, Icono = "pollo", Agotado = true },
            }
        },
        new Categoria
        {
            Id = "tacos", Nombre = "Tacos", Nota = "Por unidad o en combo", Icono = "taco",
            Productos = new List<Producto>
            {
                new Producto { Id = "tp", Nombre = "Taco de pollo crispy", Descripcion = "Pollo frito, repollo y salsa.", Precio = 3600, Icono = "taco" },
                new Producto { Id = "tc", Nombre = "Taco de carne", Descripcion = "Carne, cebolla, cilantro y salsa.", Precio = 3800, Icono = "taco" },
                new Producto { Id = "t3", Nombre = "Combo 3 tacos", Descripcion = "3 tacos a elección y papas.", Precio = 10900, Icono = "taco", Etiqueta = "Para 1", Destacado = true },
                new Producto { Id = "t6", Nombre = "Combo 6 tacos", Descripcion = "6 tacos a elección y papas grandes.", Precio = 19900, Icono = "taco", Etiqueta = "Para 2", Destacado = true },
            }
        },
        new Categoria
        {
            Id = "extras", Nombre = "Extras y bebidas", Nota = "Para acompañar", Icono = "papas",
            Productos = new List<Producto>
            {
                new Producto { Id = "pa", Nombre = "Papas fritas", Descripcion = "Porción individual.", Precio = 3500, Icono = "papas" },
                new Producto { Id = "sa", Nombre = "Salsa extra", Descripcion = "Pedí la que quieras.", Precio = 600, Icono = "salsa" },
                new Producto { Id = "ga", Nombre = "Gaseosa 500 ml", Descripcion = "Línea a elección.", Precio = 2200, Icono = "gaseosa" },
                new Producto { Id = "ag", Nombre = "Agua 500 ml", Descripcion = "Con o sin gas.", Precio = 1800, Icono = "gaseosa" },
            }
        },
    };

    public static List<Zona> Zonas = new()
    {
        new Zona { Id = "z1", Nombre = "Zona 1", Costo = 1000 },
        new Zona { Id = "z2", Nombre = "Zona 2", Costo = 1800 },
    };

    // Busca un producto por su id en todas las categorías. Devuelve null si no existe.
    public static Producto? BuscarProducto(string id)
    {
        foreach (var categoria in Categorias)
        {
            foreach (var producto in categoria.Productos)
            {
                if (producto.Id == id) return producto;
            }
        }
        return null;
    }

    // Busca una zona de entrega por su id. Devuelve null si no existe.
    public static Zona? BuscarZona(string id)
    {
        foreach (var zona in Zonas)
        {
            if (zona.Id == id) return zona;
        }
        return null;
    }
}
