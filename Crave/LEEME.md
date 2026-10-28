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
| `Program.cs` | Arranca el servidor y define las direcciones de la API: `GET /api/menu`, `GET /api/franjas`, `POST /api/pedidos` y `GET /api/seguimiento/{codigo}`. |
| `Data/CraveDb.cs` | Conecta C# con las tablas de Supabase (categorias, productos, zonas, pedidos, pedido_items). |
| `Services/PedidoService.cs` | Valida el pedido y calcula subtotal, envío, total y vuelto. |
| `Services/HorarioService.cs` | Horarios de entrega con cupo. Junta los datos de la base; la cuenta está en `GeneradorFranjas.cs`. |
| `Services/PedidoReglas.cs` | Largos máximos, caracteres permitidos y detección de spam del pedido. |
| `Endpoints/Limites.cs` | Límites de velocidad por IP (login, pedidos, lectura). |
| `Services/LatidoBaseDeDatos.cs` | Consulta la base cada 6 horas para que Supabase no pause el proyecto. `ChequeoBase.cs` hace la consulta. |
| `Services/ConfigService.cs` | Lee y guarda la configuración del local (tabla `configuracion`). |
| `Models/*.cs` | Las clases: `Producto`, `Categoria`, `Zona`, `Franja`, `PedidoNuevo`, `PedidoCreado` y `Pedido`/`PedidoItem` (las filas guardadas). |
| `wwwroot/index.html` | La estructura de la página. |
| `wwwroot/css/estilos.css` | Colores, tipografías y animaciones. |
| `wwwroot/js/app.js` | Carrito, formulario y animaciones. Habla con la API. |
| `wwwroot/js/menu-demo.js` | Carta de ejemplo, solo para ver la página sin servidor (`MODO_DEMO = true` en `app.js`). |
| `sql/02-menu-real.sql` | Carga la carta real (categorías, productos, fotos) y las columnas de gaseosas a elección. No toca los pedidos. |
| `sql/05-pago-transferencia.sql` | Agrega `pedidos.pago_confirmado_en` y `configuracion.minutos_pago_demorado`. Correrlo ANTES de subir el código de la confirmación de pago. |
| `Models/EstadoPago.cs` | Estados de pago de un pedido (pendiente, demorado, confirmado...) y las reglas para confirmarlo. |
| `sql/06-seguimiento.sql` | Agrega `pedidos.codigo_seguimiento` (con índice único). Correrlo ANTES de subir el código del seguimiento. |
| `Services/SeguimientoReglas.cs` | Crea el código de seguimiento al azar y revisa los que llegan de afuera. |
| `Services/SeguimientoService.cs` | Busca un pedido por su código y devuelve solo lo que puede ver el cliente (`Models/Seguimiento.cs`). |
| `wwwroot/seguimiento.html`, `css/seguimiento.css`, `js/seguimiento.js` | La página donde el cliente sigue su pedido. |
| `wwwroot/img/productos/` | Fotos de los productos. |
| `sql/07-fotos-faltantes.sql` | Completa la columna `imagen` de los productos que todavía no tienen foto (Familiar para 6, tacos). |
| `sql/08-lanzamiento.sql` | Precios reales y de lanzamiento, tacos "Próximamente" y el interruptor de la promo. Correrlo ANTES de subir el código de la semana de lanzamiento. |
| `sql/09-envios-y-horarios.sql` | Carga los barrios con su costo de envío y deja los horarios de 20:00 a 22:30, cada 30 minutos, 5 pedidos por horario. |
| `Services/Precios.cs` | La única regla de precios: qué se cobra (normal o de lanzamiento), qué se tacha y el porcentaje de descuento. |
| `Services/ArmarCarta.cs` | Arma la carta que recibe la página (`Models/Carta.cs`) con los precios de hoy ya calculados. |
| `wwwroot/favicon.ico`, `favicon.svg`, `apple-touch-icon.png`, `icon-192.png`, `icon-512.png`, `site.webmanifest` | Ícono de la pestaña, del iPhone y de Android (la "A" roja del logo sobre negro). |
| `wwwroot/img/og-crave.jpg` | Imagen que se ve al compartir el enlace en WhatsApp, Instagram o Facebook (1200 x 630). |

## Cómo se conecta todo

1. La página pide `/api/menu` (la carta, leída de la base) y `/api/franjas` (los horarios con lugares libres).
2. La persona arma el pedido. El navegador solo calcula el total para mostrarlo.
3. Al confirmar, la página manda a `/api/pedidos` los productos y cantidades (sin precios) y los datos de entrega y el horario elegido.
4. `PedidoService.Validar` revisa todo. Si hay problemas, responde 400 con la lista y la página los muestra.
5. Si está bien, `PedidoService.Crear` vuelve a revisar el cupo del horario (por si otro cliente se llevó el último lugar), calcula los importes con los precios de la tabla `productos`, guarda el pedido en la base (dentro de una transacción con candado por horario) y lo devuelve.

## Envíos (no hay retiro en el local)

CRAVE solo hace envíos a domicilio: no hay retiro en el local. Por eso el formulario no pregunta "delivery o retiro", siempre pide dirección y barrio, y el servidor guarda todos los pedidos con `modo = "delivery"` (la columna sigue en la tabla `pedidos`).
**Si en el futuro se toca el formulario o los textos, no volver a agregar la opción de retiro.**

El costo del envío depende del barrio. Cada barrio es una fila de la tabla `zonas` (se cargan con `sql/09-envios-y-horarios.sql`):

| Zona | Barrios | Envío |
|---|---|---|
| Cercana | Colón, 9 de Julio, Villa del Parque | $1000 |
| Media | Centro, Barrio Sancor | $1500 |
| Lejos | Moreno, Cooperativo, Zona Sur, Ciudad Verde, Villa Autódromo | $2000 |

- El cliente elige su barrio en una lista (ordenada por costo). No hay opción "Otro": solo se aceptan los barrios de la tabla.
- La dirección que se guarda termina con el nombre del barrio, ej.: "Bv. Ejemplo 1234 (2°B), Centro".
- Para cambiar un costo o agregar un barrio: Supabase -> Table Editor -> `zonas`. Se ve al recargar la página.

## Datos para la transferencia

El alias, el CBU, el titular y el banco (cuenta de Naranja X) están en `CONFIG`, al principio de `wwwroot/js/app.js`. Se puede transferir desde cualquier banco o billetera, también Mercado Pago. Si cambia la cuenta, se cambia solo ahí.

## Horarios con cupo

El cliente elige un horario de una grilla en vez de "lo antes posible". Cada horario tiene un cupo de pedidos; cuando se llena, aparece tachado como "Completo".
Hoy: de 20:00 a 22:30, uno cada 30 minutos, 5 pedidos por horario (6 horarios x 5 = 30 pedidos por día como máximo). Si hace falta más lugar, se sube "Pedidos por horario" desde el panel.
Se configura desde el panel, pestaña "Horarios" (los valores se guardan en la tabla `configuracion`, una sola fila):

| Campo | Qué controla |
|---|---|
| Minutos de preparación | El primer horario ofrecido es "ahora + estos minutos". |
| Minutos entre horarios | Cada cuántos minutos hay un horario (30). |
| Pedidos por horario | Cuántos pedidos entran en cada horario (5). |
| Primer / último horario | Desde y hasta qué hora se ofrecen horarios (20:00 y 22:30). |
| Mensaje de local cerrado | Lo que ven los clientes cuando el local está cerrado. |

Arriba de todo el panel hay un botón "Cerrar local" / "Abrir local". Con el local cerrado la tienda muestra un cartel con el mensaje, no ofrece horarios y el servidor rechaza cualquier pedido.
Para que esto funcione hay que correr una vez `sql/04-configuracion.sql` en Supabase. Si la tabla todavía no existe, el servidor usa los valores de `Models/Configuracion.cs` (20:00 a 22:30, cada 30 minutos, 5 pedidos, 30 de preparación).
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

## Que el sitio no se duerma (Render) ni se pause (Supabase)

Hay dos "dormidas" distintas en los planes gratuitos:

| Servicio | Qué pasa | Cómo se evita |
|---|---|---|
| Render (gratis) | Si pasan 15 minutos sin visitas, el servidor se apaga. La próxima visita lo despierta, pero tarda cerca de 1 minuto. | Un monitor externo que visite `/api/salud` cada 5 minutos, o el plan Starter (USD 7 por mes), que no se duerme. |
| Supabase (gratis) | Si pasa una semana con poca actividad, pausa el proyecto (los datos no se borran; se reactiva desde el panel de Supabase). Avisa por mail una semana antes. | `/api/salud` y el latido interno consultan la base. Con Render pago alcanza el latido solo. |

Qué hay en el código:

- `GET /api/salud`: responde 200 si el servidor y la base funcionan, o 503 si la base falla. Acepta GET y HEAD. No devuelve datos.
- `Services/LatidoBaseDeDatos.cs`: cada 6 horas consulta la base (también al arrancar). Se cambia con la variable `Latido__CadaSegundos`. En el plan gratuito de Render no corre mientras el servidor duerme, por eso hace falta además el monitor externo.
- Si la base falla, el latido lo anota en el log ("Latido a la base de datos: falló") y reintenta; no tira abajo el servidor.

Monitor gratuito con UptimeRobot (50 monitores, chequeo cada 5 minutos):

1. Crear cuenta en uptimerobot.com y entrar a Dashboard -> Add New Monitor.
2. Tipo "HTTP(s)", nombre "CRAVE", URL `https://cravesunchales.onrender.com/api/salud`, intervalo 5 minutos.
3. En alertas, poner tu mail: avisa si el sitio o la base dejan de responder.

El plan gratuito de Render da 750 horas por mes entre todos los servicios gratuitos de la cuenta. Un servicio despierto todo el mes usa unas 744, así que entra solo si es el único servicio gratuito de la cuenta.

## Cambiar la carta

Se hace desde Supabase, sin tocar código: Table Editor -> `productos`. Para marcar algo como agotado, poné `agotado` en true (o usá el panel). Para cambiar un precio, editá `precio` (normal) o `precio_promo` (lanzamiento). Los cambios se ven al recargar la página.
Los precios son los de la lista del cliente del 27/10/2026 (`sql/08-lanzamiento.sql`).

## Semana de lanzamiento (precios de lanzamiento)

Cada producto tiene dos precios: `precio` (el normal) y `precio_promo` (el de lanzamiento; vacío = sin promo).
Cuál se cobra lo decide un solo interruptor: `configuracion.promo_activa`, que se prende y se apaga desde el panel
con el cartel "Precios de lanzamiento" (se ve en todas las pestañas). Empieza apagado.

Con la promo **prendida**:
- `/api/menu` manda `precio` = el de lanzamiento, `precioAntes` = el normal y `descuento` = el porcentaje (redondeado para abajo, para no prometer de más). Lo arma `Services/ArmarCarta.cs` con la regla de `Services/Precios.cs`.
- La tienda muestra el precio normal tachado, un estallido rojo con el porcentaje en cada tarjeta, la cinta con "Semana de lanzamiento / Precios de apertura / Hasta X% OFF", un estallido "Hasta X% OFF" en el sello de la portada y, en el cajón, cuánto se ahorra.
- `PedidoService` cobra el precio de lanzamiento y lo guarda en cada renglón (`pedido_items.precio`).

Con la promo **apagada** la página se ve como siempre y se cobra el precio normal. No hace falta borrar nada para terminar la promo.
Los textos de la cinta están en `CONFIG.promo` de `wwwroot/js/app.js`. La base no acepta un `precio_promo` mayor o igual que el normal (regla `precio_promo_valido`).

**Tacos "Próximamente":** la columna `proximamente` muestra el producto con una franja gris y sin botón para agregarlo; el servidor también lo rechaza. Cuando salgan los tacos: cargar sus precios reales y correr `update productos set proximamente = false where categoria_id = 'tacos';`.

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

## Confirmación de pago por transferencia

Un pedido por transferencia no está pago hasta que el dueño mira su cuenta y toca "Transferencia recibida". Es la defensa fuerte contra los pedidos falsos: se ve a simple vista cuáles todavía no pagaron.
Para que funcione hay que correr una vez `sql/05-pago-transferencia.sql` en Supabase, ANTES de subir el código (si no, el panel y la tienda dan error porque buscan columnas que todavía no existen).

| Estado de pago | Cuándo | Cómo se ve en el panel |
|---|---|---|
| `efectivo` | El pedido es en efectivo | Gris: "Efectivo: cobra al entregar" (con cuánto abona y el vuelto). |
| `pendiente` | Transferencia sin confirmar, hace menos de N minutos | Recuadro y borde rojos: "Sin pago confirmado". |
| `demorado` | Transferencia sin confirmar, hace N minutos o más | Rojo fuerte: "Pago demorado". |
| `confirmado` | Tocaste "Transferencia recibida" | Verde: "Pago confirmado" con la hora. |
| `sin_pago` | Transferencia cancelada sin confirmar | Gris: "Cancelado sin pago". |

- N (por defecto 20) se cambia en la pestaña "Horarios", sección "Pagos por transferencia". El pedido nunca se cancela solo: solo se resalta.
- El estado no se guarda: se calcula con `pedidos.pago_confirmado_en` (vacío = sin confirmar), el medio de pago y la hora del pedido (`Models/EstadoPago.cs`). Por eso un pedido pasa a "demorado" solo, sin ningún proceso en segundo plano.
- "Empezar a preparar" un pedido sin pago confirmado pide un segundo toque (no lo bloquea: a un cliente conocido se le puede preparar antes).
- Cancelar un pedido ya pagado avisa que hay que devolver la plata, y la tarjeta cancelada lo recuerda.
- "Deshacer pago confirmado" corrige un toque equivocado. No se puede cambiar el pago de un pedido cancelado ni de uno en efectivo (el servidor responde 409).
- El botón "Sin pago (N)" de la lista de pedidos junta todas las transferencias sin confirmar, incluso las ya entregadas.
- Los pedidos que ya estaban en la base antes de este cambio quedan "sin confirmar". Se borran con el `truncate` previo al lanzamiento.
- Endpoint: `PATCH /api/admin/pedidos/{id}/pago` con `{ "confirmado": true }` (o `false` para deshacer).

## Seguimiento del pedido para el cliente

Al confirmar un pedido, el cliente recibe un enlace `/seguimiento.html?c=CODIGO` (botón "Seguir mi pedido" y línea "Seguí tu pedido" en el mensaje de WhatsApp). La página muestra en qué paso está su pedido (recibido, preparando, listo, entregado, o cancelado) y se actualiza sola cada 15 segundos hasta que se entrega o se cancela.
Para que funcione hay que correr una vez `sql/06-seguimiento.sql` en Supabase, ANTES de subir el código.

- **Por qué un código y no el número CR-1001:** el número es correlativo, así que cualquiera podría probar CR-1002, CR-1003... y mirar pedidos ajenos. El código tiene 12 letras y números al azar (`SeguimientoReglas.NuevoCodigo`, con `RandomNumberGenerator`, el azar de seguridad de .NET): más de 7 x 10^17 combinaciones. Sin el enlace no hay forma práctica de llegar al pedido.
- **Qué devuelve `GET /api/seguimiento/{codigo}`:** número, estado, horario, medio de pago, si la transferencia está confirmada y el total. Nunca nombre, teléfono, dirección, referencias ni aclaraciones (clase `SeguimientoPedido`).
- Un código mal escrito o inexistente responde siempre 404 igual. Límite: 30 consultas por minuto por IP (`Limites.PoliticaSeguimiento`); la página consulta 4 por minuto.
- Si la transferencia no está confirmada, la página le recuerda al cliente que mande el comprobante por el chat de WhatsApp donde envió el pedido. Esto usa lo que se marca en el panel (ver arriba).
- Los pedidos hechos antes de este cambio no tienen código ni página de seguimiento.
- La página no tiene botón de WhatsApp propio para no repetir el número en un segundo lugar: el cliente usa el chat donde ya mandó el pedido.

## Fotos de los productos

Cada producto tiene una columna `imagen` en la tabla `productos` (la ruta del archivo dentro de `wwwroot`). Si está vacía, la tarjeta muestra un dibujo provisorio. Si la ruta existe pero el archivo no carga, también se muestra el dibujo (`app.js`, escuchador de "error"): nunca queda una imagen rota.

Faltan 5 fotos. La del Familiar para 6 es la que importa ahora; los tacos están como "Próximamente" y pueden esperar. (Las alitas salieron de la carta.)

| Producto (id) | Archivo que hay que crear en `wwwroot/img/productos/` |
|---|---|
| Familiar para 6 (`fam-6`) | `familiar-6.jpg` |
| Taco de pollo crispy (`tac-pollo`) | `taco-pollo.jpg` |
| Taco de carne (`tac-carne`) | `taco-carne.jpg` |
| Combo 3 tacos (`tac-combo3`) | `combo-3-tacos.jpg` |
| Combo 6 tacos (`tac-combo6`) | `combo-6-tacos.jpg` |

Cómo preparar cada foto (para que se vea igual que las 20 que ya están):

- Horizontal, proporción 4:3, de 800 x 600 píxeles. Las que hay hoy miden 800 x 600 (la tarjeta recorta lo que sobre, así que el producto va al centro).
- Formato JPG y menos de 100 KB. Una foto de celular pesa varios MB y haría lenta la carta. Para achicarla: squoosh.app, arrastrar la foto, "Resize" a ancho 800, formato "MozJPEG", calidad cerca de 75, y bajar el resultado.
- Mismo estilo que las actuales: fondo oscuro y el producto bien iluminado.
- Nombre del archivo en minúsculas, sin espacios ni tildes, igual al de la tabla (en el servidor de Render las mayúsculas importan: `Alitas.jpg` no es `alitas.jpg`).

Pasos para cargar una foto:

1. Copiar el archivo en `wwwroot/img/productos/`.
2. Hacer commit y push (la foto viaja con el código a Render).
3. Cuando Render termine de publicar, correr en Supabase las líneas de ese producto de `sql/07-fotos-faltantes.sql` (cambia solo la columna `imagen`). Los tacos vienen comentados: sacales los `--` cuando tengas sus fotos.
4. Recargar la tienda con Ctrl+F5. La consulta del final del SQL muestra qué productos siguen sin foto.

`sql/02-menu-real.sql` reemplaza la carta completa: si lo volvés a correr, las fotos del 07 se pierden y hay que correr el 07 de nuevo.

## Ícono y vista previa al compartir

- **Ícono:** la "A" roja del logo sobre fondo negro. `favicon.ico` y `favicon.svg` son los de la pestaña del navegador (antes `/favicon.ico` daba 404). `apple-touch-icon.png` es el que usa el iPhone al guardar la página en la pantalla de inicio, y `icon-192.png` / `icon-512.png` con `site.webmanifest`, el de Android. Se enlazan desde el `<head>` de la tienda, del seguimiento y del panel.
- **Vista previa al compartir:** cuando se pega el enlace en WhatsApp, Instagram o Facebook aparece `img/og-crave.jpg` con el título y la descripción que están en las etiquetas `og:` de `index.html`. La página de seguimiento usa la misma imagen.
- **Dominio propio:** `og:url` y `og:image` de `index.html` (y `og:image` de `seguimiento.html`) llevan la dirección completa `https://cravesunchales.onrender.com`: los servicios que arman la vista previa no aceptan rutas relativas. Cuando tengas dominio, cambiala en esos tres lugares.
- **La vista previa se guarda en caché:** WhatsApp y Facebook recuerdan la imagen de un enlace durante días. Si compartís el enlace antes de que esté todo listo y después cambiás la imagen, el chat puede seguir mostrando la vieja. Para probar, mandate el enlace a vos mismo; para forzar la actualización en Facebook existe el "Depurador de uso compartido" (developers.facebook.com/tools/debug).

## Pendiente

- Precios de los tacos (hoy son provisorios y no se muestran).
- Confirmar con el cliente: "XL" en las hamburguesas grandes, que el Combo kids cheddar lleva gaseosa y si las XL incluyen papas o gaseosa.
- Fotos que faltan: ver la tabla de "Fotos de los productos".
