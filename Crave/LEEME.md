# CRAVE - web de pedidos

Proyecto ASP.NET Core (Minimal API) que sirve la página y calcula los pedidos en C#.

## Cómo ejecutarlo

1. Abrí una terminal en esta carpeta (la que tiene `Crave.csproj`).
2. Ejecutá `dotnet --version`. Si no empieza con 10, abrí `Crave.csproj` y cambiá `net10.0` por `net8.0` o `net9.0`, según lo que muestre.
3. Ejecutá `dotnet run`.
4. Copiá la dirección que aparece en `Now listening on: http://localhost:XXXX` y abrila en el navegador.

Si `dotnet run` muestra un error, copiá el mensaje completo y mandámelo.

## Qué hace cada archivo

| Archivo | Para qué sirve |
|---|---|
| `Program.cs` | Arranca el servidor y define las dos direcciones de la API: `GET /api/menu` y `POST /api/pedidos`. |
| `Data/MenuData.cs` | La carta: categorías, productos, precios y zonas de entrega. Para cambiar un precio, editá acá. |
| `Services/PedidoService.cs` | Valida el pedido y calcula subtotal, envío, total y vuelto. |
| `Models/*.cs` | Las clases: `Producto`, `Categoria`, `Zona`, `PedidoNuevo`, `PedidoCreado`. |
| `wwwroot/index.html` | La estructura de la página. |
| `wwwroot/css/estilos.css` | Colores, tipografías y animaciones. |
| `wwwroot/js/app.js` | Carrito, formulario y animaciones. Habla con la API. |
| `wwwroot/js/menu-demo.js` | Carta de ejemplo, solo para ver la página sin servidor (`MODO_DEMO = true` en `app.js`). |

## Cómo se conecta todo

1. La página pide `/api/menu` y dibuja la carta con lo que devuelve `MenuData`.
2. La persona arma el pedido. El navegador solo calcula el total para mostrarlo.
3. Al confirmar, la página manda a `/api/pedidos` los productos y cantidades (sin precios) y los datos de entrega.
4. `PedidoService.Validar` revisa todo. Si hay problemas, responde 400 con la lista y la página los muestra.
5. Si está bien, `PedidoService.Crear` calcula los importes con los precios de `MenuData`, guarda el pedido en memoria y lo devuelve.

## Pendiente

- Guardar la carta y los pedidos en SQL Server (en lugar de `MenuData` y de la lista en memoria).
- Panel del local para ver pedidos y cambiar su estado.
- Fotos reales de producto: copiarlas en `wwwroot/img` y completar `Imagen = "img/archivo.png"` en `MenuData.cs`.
- Número de WhatsApp, alias y titular reales: se cambian en `CONFIG` dentro de `wwwroot/js/app.js`.
