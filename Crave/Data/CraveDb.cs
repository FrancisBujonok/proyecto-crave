using Crave.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Crave.Data;

// Esta clase representa la base de datos de Supabase. Cada DbSet es una tabla.
// Para pedir datos se escribe algo como:  db.Productos.Where(p => p.Precio < 5000).ToListAsync()
public class CraveDb : DbContext, IDataProtectionKeyContext
{
    public CraveDb(DbContextOptions<CraveDb> opciones) : base(opciones) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();   // una sola fila: la configuración del local
    public DbSet<Salsa> Salsas => Set<Salsa>();   // las salsas que se eligen dentro de los combos

    // Claves con las que ASP.NET firma la cookie de sesión del panel. Se guardan en la base
    // porque el servidor de Render se borra y se recrea en cada deploy: si las claves vivieran en el disco,
    // cada deploy cerraría la sesión del dueño.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // Acá le explicamos a Entity Framework cómo se llaman las tablas y las relaciones
    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Categoria>().ToTable("categorias");
        modelo.Entity<Producto>().ToTable("productos");
        modelo.Entity<Zona>().ToTable("zonas");
        modelo.Entity<Pedido>().ToTable("pedidos");
        modelo.Entity<PedidoItem>().ToTable("pedido_items");
        modelo.Entity<DataProtectionKey>().ToTable("claves_sesion");
        modelo.Entity<Configuracion>().ToTable("configuracion");
        modelo.Entity<Salsa>().ToTable("salsas");
        modelo.Entity<Configuracion>().Property(c => c.Id).ValueGeneratedNever();   // el id siempre es 1, no lo numera la base

        // Una categoría tiene muchos productos
        modelo.Entity<Categoria>()
            .HasMany(c => c.Productos).WithOne().HasForeignKey(p => p.CategoriaId);

        // Un pedido tiene muchos renglones
        modelo.Entity<Pedido>()
            .HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PedidoId);

        // En C# las propiedades se escriben AbonaCon, pero en la base las columnas son abona_con.
        // Este bucle convierte los nombres de todas las columnas a minúsculas con guiones bajos.
        foreach (var tabla in modelo.Model.GetEntityTypes())
        {
            foreach (var propiedad in tabla.GetProperties())
            {
                propiedad.SetColumnName(ASnakeCase(propiedad.Name));
            }
        }
    }

    // "CreadoEn" -> "creado_en"
    static string ASnakeCase(string nombre)
    {
        var resultado = new System.Text.StringBuilder();
        for (int i = 0; i < nombre.Length; i++)
        {
            char letra = nombre[i];
            if (char.IsUpper(letra) && i > 0) resultado.Append('_');
            resultado.Append(char.ToLower(letra));
        }
        return resultado.ToString();
    }
}
