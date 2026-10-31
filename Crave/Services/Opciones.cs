namespace Crave.Services;

// Las "opciones" son lo que el cliente elige dentro de un combo al tocar "+": las gaseosas y las salsas.
// Las dos funcionan igual: cada producto dice cuántas se eligen por unidad (gaseosas_a_elegir, salsas_a_elegir),
// se pueden repetir, y el cliente puede tocar "No quiero gaseosa" / "No quiero salsa".
// Esta clase tiene las reglas, sin tocar la base de datos, así se puede probar sola.
public static class Opciones
{
    // Lo único que hace falta saber de cada gaseosa o salsa para revisar el pedido
    public record Opcion(string Nombre, bool Agotado);

    // Cómo se nombra cada grupo en los mensajes y en el detalle del pedido
    public record Grupo(string Singular, string Plural);

    public static readonly Grupo Gaseosas = new("gaseosa", "gaseosas");
    public static readonly Grupo Salsas = new("salsa", "salsas");

    // Revisa lo elegido de un grupo en un renglón del pedido y agrega los problemas a "errores".
    //   porUnidad: cuántas se eligen por cada unidad del producto (0 = el producto no lleva de este grupo)
    //   sinElegir: true si el cliente tocó "No quiero ..." (entonces la lista tiene que venir vacía)
    public static void Validar(string producto, int porUnidad, int cantidad, List<string> elegidas, bool sinElegir,
        Dictionary<string, Opcion> disponibles, Grupo grupo, List<string> errores)
    {
        if (porUnidad == 0)
        {
            if (elegidas.Count > 0)
                errores.Add(producto + " no lleva " + grupo.Singular + " a elección.");
            return;   // si llega "No quiero..." en un producto que no lleva, no molesta: se ignora
        }

        if (sinElegir)
        {
            if (elegidas.Count > 0)
                errores.Add("En " + producto + " elegiste \"No quiero " + grupo.Singular + "\" y también " + grupo.Plural + ". Revisalo.");
            return;
        }

        if (elegidas.Count != porUnidad * cantidad)
        {
            errores.Add("Elegí las " + grupo.Plural + " de " + producto + ".");
            return;
        }

        foreach (var id in elegidas)
        {
            if (id == null || !disponibles.TryGetValue(id, out var opcion))
                errores.Add("Hay una " + grupo.Singular + " que ya no está en la carta.");
            else if (opcion.Agotado)
                errores.Add(opcion.Nombre + " está agotada.");   // gaseosa y salsa son femeninas: sirve para las dos
        }
    }

    // El texto de un grupo para el detalle del pedido (lo ven el panel, el cliente y el WhatsApp).
    // Ejemplos: "Gaseosa: Sprite"  "Salsas: 2 × CRAVE, 1 × Hot mayo"  "Sin salsa"
    public static string Texto(List<string> elegidas, bool sinElegir, Dictionary<string, Opcion> disponibles, Grupo grupo)
    {
        if (sinElegir || elegidas.Count == 0)
            return "Sin " + grupo.Singular;

        string titulo = Mayuscula(elegidas.Count == 1 ? grupo.Singular : grupo.Plural);
        if (elegidas.Count == 1)
            return titulo + ": " + disponibles[elegidas[0]].Nombre;

        // GroupBy junta las repetidas y mantiene el orden en que aparecen
        var partes = elegidas
            .GroupBy(id => id)
            .Select(repetidas => repetidas.Count() + " × " + disponibles[repetidas.Key].Nombre);
        return titulo + ": " + string.Join(", ", partes);
    }

    static string Mayuscula(string texto)
    {
        return char.ToUpper(texto[0]) + texto.Substring(1);
    }
}
