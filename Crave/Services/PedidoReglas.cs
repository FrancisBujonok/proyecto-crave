using Crave.Models;

namespace Crave.Services;

// Las reglas "de forma" del pedido y las defensas contra el spam. No usan la base de datos:
// miran el pedido tal como llegó (largos, caracteres raros, datos que faltan) y devuelven una respuesta.
// La regla es: nunca confiar en lo que manda el navegador, porque cualquiera puede mandar un pedido sin usar la página.
public static class PedidoReglas
{
    // ---------- Largos máximos (los mismos números van en los maxlength del formulario) ----------
    public const int MaxNombre = 60;
    public const int MaxTelefono = 25;
    public const int MaxCalle = 80;
    public const int MaxPiso = 30;
    public const int MaxReferencias = 200;
    public const int MaxNotas = 300;
    public const int MaxIdTexto = 50;         // ids de producto, de zona, de gaseosa y de salsa
    public const int MaxRenglones = 60;       // renglones del pedido (los combos con gaseosas van de a uno)
    public const int MaxGaseosasPorRenglon = 200;   // también es el máximo de salsas por renglón
    public const int MaxAbonaCon = 1_000_000;

    // ---------- Antispam ----------
    public const int SegundosMinimos = 8;     // nadie arma un pedido y lo confirma en menos de esto
    public const int MaxPedidosActivosPorTelefono = 3;

    public const string MensajeSospechoso = "No pudimos procesar el pedido. Recargá la página e intentá de nuevo.";

    // true si el pedido parece mandado por un programa y no por una persona:
    // 1) el campo "website" está oculto en la página: una persona no lo ve, un programa que llena todo sí lo llena.
    // 2) "segundos" es el tiempo que la página estuvo abierta: un programa manda el pedido al instante.
    public static bool EsSospechoso(PedidoNuevo pedido)
    {
        if (!string.IsNullOrWhiteSpace(pedido.Website)) return true;
        if (pedido.Segundos == null || pedido.Segundos < SegundosMinimos) return true;
        return false;
    }

    // Revisa que no falten datos, que los textos no sean enormes y que no tengan caracteres raros.
    // Devuelve la lista de problemas (vacía = la forma está bien).
    public static List<string> RevisarForma(PedidoNuevo pedido)
    {
        // El JSON puede traer "nombre": null aunque en C# la propiedad no admita null. Sin esta revisión, un null rompería el servidor.
        if (pedido.Nombre is null || pedido.Telefono is null || pedido.Calle is null
            || pedido.Piso is null || pedido.ZonaId is null || pedido.Referencias is null || pedido.Hora is null
            || pedido.Pago is null || pedido.Notas is null || pedido.Items is null)
            return new List<string> { "Faltan datos del pedido." };

        var errores = new List<string>();

        if (pedido.Items.Count > MaxRenglones)
            errores.Add("El pedido tiene demasiados productos.");

        foreach (var item in pedido.Items)
        {
            if (item is null || item.ProductoId is null)
                return new List<string> { "Faltan datos del pedido." };

            item.Gaseosas ??= new List<string>();   // sin gaseosas es lo mismo que una lista vacía
            item.Salsas ??= new List<string>();     // lo mismo con las salsas

            if (item.ProductoId.Length > MaxIdTexto
                || item.Gaseosas.Count > MaxGaseosasPorRenglon || item.Gaseosas.Any(g => g is not null && g.Length > MaxIdTexto)
                || item.Salsas.Count > MaxGaseosasPorRenglon || item.Salsas.Any(s => s is not null && s.Length > MaxIdTexto))
                errores.Add("Hay un producto con datos que no son válidos.");
        }

        Largo(errores, pedido.Nombre, MaxNombre, "El nombre");
        Largo(errores, pedido.Telefono, MaxTelefono, "El teléfono");
        Largo(errores, pedido.Calle, MaxCalle, "La calle");
        Largo(errores, pedido.Piso, MaxPiso, "El piso o departamento");
        Largo(errores, pedido.Referencias, MaxReferencias, "Las referencias", permiteSaltos: true);
        Largo(errores, pedido.Notas, MaxNotas, "Las aclaraciones", permiteSaltos: true);

        if (pedido.ZonaId.Length > MaxIdTexto || pedido.Hora.Length > 5 || pedido.Pago.Length > 20)
            errores.Add("Hay datos del pedido que no son válidos.");

        // El teléfono solo puede tener números y los signos habituales
        if (pedido.Telefono.Any(c => !(char.IsDigit(c) || c == ' ' || c == '+' || c == '-' || c == '(' || c == ')' || c == '.')))
            errores.Add("El teléfono solo puede tener números, espacios, + y guiones.");
        else if (pedido.Telefono.Count(char.IsDigit) > 15)
            errores.Add("El teléfono tiene demasiados números.");

        // Un nombre real tiene al menos dos letras (evita "12345" o "....")
        if (pedido.Nombre.Trim().Length >= 3 && pedido.Nombre.Count(char.IsLetter) < 2)
            errores.Add("Escribí tu nombre y apellido.");

        if (pedido.AbonaCon < 0 || pedido.AbonaCon > MaxAbonaCon)
            errores.Add("El importe con el que abonás no es válido.");

        return errores.Distinct().ToList();
    }

    // Largo máximo y caracteres de control (los invisibles que se usan para ensuciar datos).
    // Los saltos de línea solo se aceptan en los campos de texto largo.
    static void Largo(List<string> errores, string texto, int maximo, string campo, bool permiteSaltos = false)
    {
        if (texto.Length > maximo)
            errores.Add(campo + " es demasiado largo (máximo " + maximo + " caracteres).");
        else if (texto.Any(c => char.IsControl(c) && !(permiteSaltos && (c == '\n' || c == '\r'))))
            errores.Add(campo + " tiene caracteres que no se pueden usar.");
    }

    // Para saber si dos teléfonos son el mismo aunque estén escritos distinto:
    // "3493 41-2345", "(03493) 412345" y "+54 9 3493 412345" terminan en los mismos 8 números.
    public static string ClaveTelefono(string telefono)
    {
        string numeros = new string((telefono ?? "").Where(char.IsDigit).ToArray());
        return numeros.Length <= 8 ? numeros : numeros.Substring(numeros.Length - 8);
    }
}
