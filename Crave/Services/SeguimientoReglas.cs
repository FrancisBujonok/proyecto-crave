using System.Security.Cryptography;

namespace Crave.Services;

// El código de seguimiento: cómo se crea y cómo se revisa uno que llega de afuera. No usa la base de datos.
public static class SeguimientoReglas
{
    public const int LargoCodigo = 12;

    // Letras y números que no se confunden al leerlos: sin 0, O, 1, I ni L.
    // 31 símbolos en 12 lugares son más de 7 x 10^17 combinaciones: nadie las adivina probando.
    const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    // Un código nuevo. Usa RandomNumberGenerator (el azar "de seguridad" de .NET), no Random,
    // porque Random se puede predecir y este código es lo único que protege la privacidad del pedido.
    public static string NuevoCodigo()
    {
        var letras = new char[LargoCodigo];
        for (int i = 0; i < letras.Length; i++)
            letras[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        return new string(letras);
    }

    // Deja el código como se guarda en la base (mayúsculas, sin espacios en los extremos).
    // Devuelve null si lo que llegó no tiene la forma de un código: así ni siquiera se consulta la base.
    public static string? Normalizar(string? texto)
    {
        string codigo = (texto ?? "").Trim().ToUpperInvariant();
        if (codigo.Length != LargoCodigo) return null;
        return codigo.All(c => Alfabeto.Contains(c)) ? codigo : null;
    }
}
