# CRAVE - web de pedidos

Proyecto ASP.NET Core (Minimal API) que sirve la página y calcula los pedidos en C#. La carta y los pedidos viven en una base PostgreSQL de Supabase.

## Antes de ejecutarlo (una sola vez)

1. Crear las tablas en Supabase: SQL Editor -> pegar `crave-base-de-datos.sql` -> Run.
   Después, en el mismo lugar, pegar `sql/02-menu-real.sql` -> Run. Carga la carta real y se puede volver a correr sin perder pedidos.
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
| `Services/HorarioService.cs` | Horarios de entrega con cupo. Junta los datos de la base; la cuenta está en `GeneradorFranjas.cs`. |
| `Services/PedidoReglas.cs` | Largos máximos, caracteres permitidos y detección de spam del pedido. |
| `Endpoints/Limites.cs` | Límites de velocidad por IP (login, pedidos, lectura). |
| `Services/ConfigService.cs` | Lee y guarda la configuración del local (tabla `configuracion`). |
| `Models/*.cs` | Las clases: `Producto`, `Categoria`, `Zona`, `Franja`, `PedidoNuevo`, `PedidoCreado` y `Pedido`/`PedidoItem` (las filas guardadas). |
| `wwwroot/index.html` | La estructura de la página. |
| `wwwroot/css/estilos.css` | Colores, tipografías y animaciones. |
| `wwwroot/js/app.js` | Carrito, formulario y animaciones. Habla con la API. |
| `wwwroot/js/menu-demo.js` | Carta de ejemplo, solo para ver la página sin servidor (`MODO_DEMO = true` en `app.js`). |
| `sql/02-menu-real.sql` | Carga la carta real (categorías, productos, fotos) y las columnas de gaseosas a elección. No toca los pedidos. |
| `wwwroot/img/productos/` | Fotos de los productos. |

## Cómo se conecta todo

1. La página pide `/api/menu` (la carta, leída de la base) y `/api/franjas` (los horarios con lugares libres).
2. La persona arma el pedido. El navegador solo calcula el total para mostrarlo.
3. Al confirmar, la página manda a `/api/pedidos` los productos y cantidades (sin precios) y los datos de entrega y el horario elegido.
4. `PedidoService.Validar` revisa todo. Si hay problemas, responde 400 con la lista y la página los muestra.
5. Si está bien, `PedidoService.Crear` vuelve a revisar el cupo del horario (por si otro cliente se llevó el último lugar), calcula los importes con los precios de la tabla `productos`, guarda el pedido en la base (dentro de una transacción con candado por horario) y lo devuelve.

## Horarios con cupo

El cliente elige un horario de una grilla en vez de "lo antes posible". Cada horario tiene un cupo de pedidos; cuando se llena, aparece tachado como "Completo".
Se configura desde el panel, pestaña "Horarios" (los valores se guardan en la tabla `configuracion`, una sola fila):

| Campo | Qué controla |
|---|---|
| Minutos de preparación | El primer horario ofrecido es "ahora + estos minutos". |
| Minutos entre horarios | Cada cuántos minutos hay un horario (15). |
| Pedidos por horario | Cuántos pedidos entran en cada horario. |
| Primer / último horario | Desde y hasta qué hora se ofrecen horarios. |
| Mensaje de local cerrado | Lo que ven los clientes cuando el local está cerrado. |

Arriba de todo el panel hay un botón "Cerrar local" / "Abrir local". Con el local cerrado la tienda muestra un cartel con el mensaje, no ofrece horarios y el servidor rechaza cualquier pedido.
Para que esto funcione hay que correr una vez `sql/04-configuracion.sql` en Supabase. Si la tabla todavía no existe, el servidor usa los valores de siempre (20:00 a 23:30, cada 15 minutos, 4 pedidos, 30 de preparación).
La cuenta de los horarios está en `Services/GeneradorFranjas.cs` y las reglas del formulario en `Services/ConfigReglas.cs`.

Si dos clientes eligen el último lugar a la vez, el segundo recibe un 409 con el mensaje "Ese horario se acaba de completar" y la página le muestra los horarios actualizados.
En el modo demo (`MODO_DEMO = true`) los horarios se simulan con `DEMO_HORARIOS` en `app.js`.

## Protección contra spam y abusos

Cualquiera puede mandar un pedido a `POST /api/pedidos` sin usar la página, así que el servidor no confía en nada de lo que llega. Hay varias capas:

| Defensa | Qué hace | Dónde está |
|---|---|---|
| Límite por IP | 10 envíos de pedido cada 10 minutos, 5 intentos de login cada 10 minutos y 60 lecturas de carta u horarios por minuto. Si se pasa: 429. | `Endpoints/Limites.cs` |
| Tamaño máximo | Ningún envío puede pesar más de 64 KB (si no: 413). | `Program.cs` |
| Largos y caracteres | Nombre 60, teléfono 25, calle 80, piso 30, referencias 200, aclaraciones 300. Sin caracteres invisibles. Teléfono solo con números, espacios, + y guiones. Datos que faltan (null) no rompen el servidor. | `Services/PedidoReglas.cs` |
| Campo trampa | Un campo oculto (`website`) que una persona nunca llena. Si viene lleno, se descarta el pedido. | `index.html` y `PedidoReglas.EsSospechoso` |
| Tiempo en la página | Si el pedido llega a menos de 8 segundos de abrir la página, se descarta. | `app.js` (`segundos`) y `PedidoReglas` |
| Tope por teléfono | Un mismo teléfono no puede tener más de 3 pedidos en curso (nuevo, preparando o listo) el mismo día. | `PedidoService.Validar` |

Los números están al principio de `PedidoReglas.cs` y `Limites.cs`. Si un cliente real se queda bloqueado, el motivo queda en el log del servidor (Render, pestaña Logs) con la palabra "antispam".
Un spammer decidido puede saltarse algunas de estas defensas. Si alguien llena los horarios con pedidos falsos, desde el panel los cancelás y el lugar se libera.

## Cambiar la carta

Se hace desde Supabase, sin tocar código: Table Editor -> `productos`. Para marcar algo como agotado, poné `agotado` en true. Para cambiar un precio, editá `precio`. Los cambios se ven al recargar la página.
Los precios de `sql/02-menu-real.sql` son provisorios.

## Gaseosas a elección

Algunos productos (Familiar para 4, 5 y 6 y el menú niños) llevan gaseosas a elección. Se define con la columna `gaseosas_a_elegir` de `productos` (0 = ninguna).
Las opciones son los productos de la categoría `bebidas`: si marcás una como agotada, deja de ofrecerse en todos los combos.
El cliente las elige en una ventana al tocar "+", y quedan guardadas en `pedido_items.detalle` (ejemplo: "2 × Coca-Cola, 1 × Sprite").

## Panel de administración (/admin)

Es la pantalla del local: pedidos de hoy (se actualiza sola cada 15 segundos), cambio de estado y productos agotados.

1. En Supabase, correr `sql/03-claves-sesion.sql` (una sola vez). Guarda las claves de la sesión para que un deploy no la cierre.
2. Contraseña en tu PC: `dotnet user-secrets set "Admin:Password" "tu-contraseña-larga"` (10 caracteres o más).
3. Contraseña en Render: variable de entorno `Admin__Password` (con doble guion bajo).
4. Entrar a `/admin`. Hay 5 intentos de contraseña cada 10 minutos por IP.

Estados de un pedido: nuevo -> preparando -> listo -> entregado. Cancelar libera el lugar del horario. Un cancelado no se puede reactivar.
Todo lo que está bajo `/api/admin` exige la sesión iniciada (código en `Endpoints/AdminEndpoints.cs` y `Endpoints/AdminPanelEndpoints.cs`).

## Pendiente

- Precios reales (hoy son provisorios).
- Fotos que faltan: alitas, Familiar para 6 y tacos. Copiarlas en `wwwroot/img/productos/` y completar la columna `imagen` de `productos` (ejemplo: `img/productos/alitas.jpg`).
- Ajustar con el local el cupo por horario y el horario de atención.
- Número de WhatsApp, alias y titular reales: se cambian en `CONFIG` dentro de `wwwroot/js/app.js`.
