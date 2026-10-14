# CRAVE - web de pedidos

Proyecto ASP.NET Core (Minimal API) que sirve la página y calcula los pedidos en C#. La carta y los pedidos viven en una base PostgreSQL de Supabase.

## Antes de ejecutarlo (una sola vez)

1. Crear las tablas en Supabase: SQL Editor -> pegar `crave-base-de-datos.sql` -> Run.
2. Instalar el paquete de base de datos (desde la carpeta con `Crave.csproj`; trae Entity Framework Core incluido):
   `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL`
   Si da un error de versión (NU1202), agregá `--version 9.*` u `8.*`, según lo que muestre `dotnet --version`.
3. Guardar la cadena de conexión fuera del repo:
   `dotnet user-secrets init`
   `dotnet user-secrets set "ConnectionStrings:Crave" "User Id=...;Password=...;Server=...;Port=5432;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"`

## Cómo ejecutarlo

1. Abrí una terminal en esta carpeta (la que tiene `Crave.csproj`).
2. Ejecutá `dotnet --version`. Si no empieza con 10, abrí `Crave.csproj` y cambiá `net10.0` por `net8.0` o `net9.0`, según lo que muestre.
3. Ejecutá `dotnet run`.
4. Copiá la dirección que aparece en `Now listening on: http://localhost:XXXX` y abrila en el navegador.

Si `dotnet run` muestra un error, copiá el mensaje completo y mandámelo.

## Qué hace cada archivo

| Archivo | Para qué sirve |
|---|---|
| `Program.cs` | Arranca el servidor y define las direcciones de la API: `GET /api/menu`, `GET /api/franjas` y `POST /api/pedidos`. |
| `Data/CraveDb.cs` | Conecta C# con las tablas de Supabase (categorias, productos, zonas, pedidos, pedido_items). |
| `Services/PedidoService.cs` | Valida el pedido y calcula subtotal, envío, total y vuelto. |
| `Services/HorarioService.cs` | Horarios de entrega con cupo. Los números de arriba del archivo se ajustan al ritmo de la cocina. |
| `Models/*.cs` | Las clases: `Producto`, `Categoria`, `Zona`, `Franja`, `PedidoNuevo`, `PedidoCreado` y `Pedido`/`PedidoItem` (las filas guardadas). |
| `wwwroot/index.html` | La estructura de la página. |
| `wwwroot/css/estilos.css` | Colores, tipografías y animaciones. |
| `wwwroot/js/app.js` | Carrito, formulario y animaciones. Habla con la API. |
| `wwwroot/js/menu-demo.js` | Carta de ejemplo, solo para ver la página sin servidor (`MODO_DEMO = true` en `app.js`). |

## Cómo se conecta todo

1. La página pide `/api/menu` (la carta, leída de la base) y `/api/franjas` (los horarios con lugares libres).
2. La persona arma el pedido. El navegador solo calcula el total para mostrarlo.
3. Al confirmar, la página manda a `/api/pedidos` los productos y cantidades (sin precios) y los datos de entrega y el horario elegido.
4. `PedidoService.Validar` revisa todo. Si hay problemas, responde 400 con la lista y la página los muestra.
5. Si está bien, `PedidoService.Crear` vuelve a revisar el cupo del horario (por si otro cliente se llevó el último lugar), calcula los importes con los precios de `MenuData`, guarda el pedido en la base (dentro de una transacción con candado por horario) y lo devuelve.

## Horarios con cupo

El cliente elige un horario de una grilla en vez de "lo antes posible". Cada horario tiene un cupo de pedidos; cuando se llena, aparece tachado como "Completo".
Se configura al principio de `Services/HorarioService.cs` (los valores son de ejemplo):

| Constante | Qué controla |
|---|---|
| `MinutosPreparacion` | El primer horario ofrecido es "ahora + estos minutos". |
| `MinutosPorFranja` | Cada cuántos minutos hay un horario (15). |
| `PedidosPorFranja` | Cuántos pedidos entran en cada horario. |
| `Apertura` / `UltimaFranja` | Desde y hasta qué hora se ofrecen horarios. |

Si dos clientes eligen el último lugar a la vez, el segundo recibe un 409 con el mensaje "Ese horario se acaba de completar" y la página le muestra los horarios actualizados.
En el modo demo (`MODO_DEMO = true`) los horarios se simulan con `DEMO_HORARIOS` en `app.js`.

## Cambiar la carta

Se hace desde Supabase, sin tocar código: Table Editor -> `productos`. Para marcar algo como agotado, poné `agotado` en true. Para cambiar un precio, editá `precio`. Los cambios se ven al recargar la página.

## Pendiente

- Panel del local para ver pedidos y cambiar su estado.
- Fotos reales de producto: copiarlas en `wwwroot/img` y completar `Imagen = "img/archivo.png"` en `MenuData.cs`.
- Ajustar con el local el cupo por horario y el horario de atención.
- Número de WhatsApp, alias y titular reales: se cambian en `CONFIG` dentro de `wwwroot/js/app.js`.
