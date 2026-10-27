-- =====================================================================
-- CRAVE - semana de lanzamiento
-- Precios nuevos, precios de lanzamiento y tacos con la franja "Próximamente".
--
-- ORDEN (importante):
--   1) Pegá este archivo entero en el SQL Editor de Supabase y tocá "Run".
--   2) Recién después subí el código nuevo (commit y push). El programa nuevo lee las columnas
--      precio_promo, proximamente y promo_activa: si todavía no existen, la carta y el panel dan error.
--
-- Qué hace:
--   - Agrega las columnas nuevas (si ya existen, no pasa nada).
--   - Carga la lista de precios del cliente (27/10/2026): el precio normal va en "precio" y el de
--     lanzamiento en "precio_promo". Los productos que ya estaban se actualizan y los que faltan se agregan.
--   - Borra los productos que ya no se venden (6 alitas + papas, Para 2: alitas, Para 3: alitas).
--   - Pone los tacos como "Próximamente": se ven en la carta pero no se pueden pedir.
--   - NO prende los precios de lanzamiento. Eso se hace desde el panel, con el botón
--     "Activar precios de lanzamiento", el día que abran.
--   - No toca pedidos ni zonas, ni las fotos que ya tengan los productos, ni el "agotado" de cada uno
--     (salvo los tacos, que dejan de figurar como agotados porque ahora dicen "Próximamente").
-- Se puede correr más de una vez.
-- =====================================================================

-- ---------- 1) Columnas nuevas ----------
-- Cuántas gaseosas elige el cliente en cada producto (0 = ninguna)
alter table productos     add column if not exists gaseosas_a_elegir int not null default 0;
-- Texto con las gaseosas elegidas en cada renglón del pedido, ej.: "2 × Coca-Cola, 1 × Sprite"
alter table pedido_items  add column if not exists detalle text;
-- Precio de lanzamiento (o de cualquier promo). Vacío = el producto no tiene precio de promo.
-- Solo se cobra cuando la promo está prendida en el panel (columna promo_activa de configuracion).
alter table productos     add column if not exists precio_promo int;
-- true = el producto se ve con la franja "Próximamente" y todavía no se puede pedir (los tacos)
alter table productos     add column if not exists proximamente boolean not null default false;
-- El interruptor de la promo. Empieza apagado: se prende desde el panel.
alter table configuracion add column if not exists promo_activa boolean not null default false;

-- El precio de promo tiene que ser mayor que 0 y menor que el normal. Si alguien carga otra cosa, la base lo rechaza.
alter table productos drop constraint if exists precio_promo_valido;
alter table productos add constraint precio_promo_valido
  check (precio_promo is null or (precio_promo > 0 and precio_promo < precio));

-- ---------- 2) Carta ----------
begin;

-- Categorías: se actualizan los textos (on conflict = "si ya existe, actualizala")
insert into categorias (id, nombre, nota, icono, orden) values
  ('individual', 'Individual', 'Para una persona', 'pollo', 1),
  ('compartir', 'Para compartir', 'Para 2 y 3 personas', 'balde', 2),
  ('familiar', 'Familiar', 'Para 4, 5 y 6 personas', 'balde', 3),
  ('ninos', 'Menú niños', 'Combos con papas y gaseosa', 'hamburguesa', 4),
  ('hamburguesas', 'Hamburguesas', 'Medallón de pollo frito', 'hamburguesa', 5),
  ('bebidas', 'Bebidas', 'Gaseosas individuales', 'gaseosa', 6),
  ('tacos', 'Tacos', 'Próximamente', 'taco', 7)
on conflict (id) do update set
  nombre = excluded.nombre, nota = excluded.nota, icono = excluded.icono, orden = excluded.orden;

-- Productos: si el id ya existe se actualiza; si no, se agrega.
-- Columnas: id, categoria_id, nombre, descripcion, precio, precio_promo, icono, imagen, etiqueta, destacado, orden, gaseosas_a_elegir, proximamente
insert into productos (id, categoria_id, nombre, descripcion, precio, precio_promo, icono, imagen, etiqueta, destacado, orden, gaseosas_a_elegir, proximamente) values
  -- INDIVIDUAL
  ('ind-pollo4', 'individual', 'Combo individual', '4 piezas de pollo frito con papas individuales.', 17900, 14000, 'pollo', 'img/productos/pollo-4-piezas.jpg', 'Para 1', true, 1, 0, false),
  ('ind-popcrave', 'individual', 'Popcrave individual', 'Porción de 10 unidades.', 9000, 7000, 'pollo', 'img/productos/popcrave.jpg', null, false, 2, 0, false),
  ('ind-papas-ch', 'individual', 'Papas con cheddar y bacon', 'Porción individual.', 9500, 8000, 'papas', 'img/productos/papas-cheddar-bacon.jpg', null, false, 3, 0, false),
  ('ind-aros', 'individual', 'Aros de cebolla', 'Porción de 8 unidades.', 6000, 5000, 'papas', 'img/productos/aros-cebolla.jpg', null, false, 4, 0, false),

  -- PARA COMPARTIR
  ('cmp-pollo8', 'compartir', 'Combo para 2', '8 piezas de pollo crujiente + 2 papas.', 35000, 29900, 'balde', 'img/productos/combo-2-personas.jpg', 'Para 2', true, 1, 0, false),
  ('cmp-pollo12', 'compartir', 'Combo para 3', '12 piezas de pollo crujiente + 3 papas.', 49900, 41000, 'balde', 'img/productos/combo-3-personas.jpg', 'Para 3', true, 2, 0, false),

  -- FAMILIAR (cada uno lleva tantas gaseosas a elección como personas)
  ('fam-4', 'familiar', 'Familiar para 4', '16 piezas de pollo crujiente + 4 papas + 4 gaseosas a elección.', 65000, 55000, 'balde', 'img/productos/familiar-4.jpg', 'Para 4', true, 1, 4, false),
  ('fam-5', 'familiar', 'Familiar para 5', '20 piezas de pollo crujiente + 5 papas + 5 gaseosas a elección.', 78000, 65000, 'balde', 'img/productos/familiar-5.jpg', 'Para 5', false, 2, 5, false),
  ('fam-6', 'familiar', 'Familiar para 6', '24 piezas de pollo crujiente + 6 papas + 6 gaseosas a elección.', 92000, 77000, 'balde', null, 'Para 6', false, 3, 6, false),

  -- MENÚ NIÑOS (todos con papas y una gaseosa a elección)
  ('nin-popcrave', 'ninos', 'Combo kids popcrave', 'Popcrave, papas y gaseosa chica a elección.', 13500, 11000, 'pollo', 'img/productos/combo-ninos-popcrave.jpg', 'Niños', false, 1, 1, false),
  ('nin-simple', 'ninos', 'Combo kids cheddar', 'Hamburguesa con cheddar + papas + gaseosa a elección.', 13900, 10900, 'hamburguesa', 'img/productos/mini-crave-ninos.jpg', 'Niños', false, 2, 1, false),
  ('nin-fresh', 'ninos', 'Combo kids fresh', 'Fresh chicken mediana + papas + gaseosa a elección.', 14500, 11500, 'hamburguesa', 'img/productos/fresh-chicken-ninos.jpg', 'Niños', false, 3, 1, false),
  ('nin-crispy', 'ninos', 'Combo kids crispy club', 'Crispy club mediana + papas + gaseosa a elección.', 14500, 11500, 'hamburguesa', 'img/productos/crispy-club-ninos.jpg', 'Niños', false, 4, 1, false),
  ('nin-sweet', 'ninos', 'Combo kids sweet', 'Sweet crave mediana + papas + gaseosa a elección.', 14000, 11500, 'hamburguesa', 'img/productos/sweet-crave-ninos.jpg', 'Niños', false, 5, 1, false),

  -- HAMBURGUESAS
  ('ham-fresh', 'hamburguesas', 'Fresh chicken XL', 'Medallón de pollo frito, salsa ranch, salsa alioli, lechuga, pepinillos y cheddar.', 17000, 13500, 'hamburguesa', 'img/productos/fresh-chicken-grande.jpg', 'XL', false, 1, 0, false),
  ('ham-sweet', 'hamburguesas', 'Sweet crave XL', 'Medallón de pollo, jalea de barbacoa, cheddar y salsa de la casa.', 18000, 14500, 'hamburguesa', 'img/productos/sweet-crave-grande.jpg', 'XL', false, 2, 0, false),
  ('ham-crispy', 'hamburguesas', 'Crispy club XL', 'Medallón de pollo frito, lechuga, tomate, salsa de la casa, panceta y cheddar.', 17000, 13500, 'hamburguesa', 'img/productos/crispy-club-grande.jpg', 'XL', false, 3, 0, false),

  -- BEBIDAS: también son las gaseosas que se eligen dentro de los combos. Si marcás una como agotada, deja de ofrecerse en todos lados.
  ('beb-coca', 'bebidas', 'Coca-Cola', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-coca.jpg', null, false, 1, 0, false),
  ('beb-sprite', 'bebidas', 'Sprite', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-sprite.jpg', null, false, 2, 0, false),
  ('beb-fanta', 'bebidas', 'Fanta', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-fanta.jpg', null, false, 3, 0, false),
  ('beb-zero', 'bebidas', 'Coca-Cola Zero', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-zero.jpg', null, false, 4, 0, false),

  -- TACOS: se ven con la franja "Próximamente" y todavía no se pueden pedir. Sus precios son provisorios y no se muestran.
  ('tac-pollo', 'tacos', 'Taco de pollo crispy', 'Pollo frito, repollo y salsa.', 3600, null, 'taco', null, null, false, 1, 0, true),
  ('tac-carne', 'tacos', 'Taco de carne', 'Carne, cebolla, cilantro y salsa.', 3800, null, 'taco', null, null, false, 2, 0, true),
  ('tac-combo3', 'tacos', 'Combo 3 tacos', '3 tacos a elección y papas.', 10900, null, 'taco', null, 'Para 1', false, 3, 0, true),
  ('tac-combo6', 'tacos', 'Combo 6 tacos', '6 tacos a elección y papas grandes.', 19900, null, 'taco', null, 'Para 2', false, 4, 0, true)
on conflict (id) do update set
  categoria_id      = excluded.categoria_id,
  nombre            = excluded.nombre,
  descripcion       = excluded.descripcion,
  precio            = excluded.precio,
  precio_promo      = excluded.precio_promo,
  icono             = excluded.icono,
  imagen            = coalesce(excluded.imagen, productos.imagen),   -- si acá no hay foto, queda la que ya tenía
  etiqueta          = excluded.etiqueta,
  destacado         = excluded.destacado,
  orden             = excluded.orden,
  gaseosas_a_elegir = excluded.gaseosas_a_elegir,
  proximamente      = excluded.proximamente;

-- Productos que ya no están en la carta. Los pedidos viejos no se rompen: cada renglón guarda su propia copia del nombre y el precio.
delete from productos where id in ('ind-alitas6', 'cmp-alitas10', 'cmp-alitas15');

-- Los tacos ya no figuran como agotados: ahora los frena la franja "Próximamente"
update productos set agotado = false where proximamente;

commit;

-- ---------- 3) Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta. Arriba de la tabla tiene que decir 25 filas
-- (21 con precio de lanzamiento y 4 tacos con proximamente = true). Revisalos precio por precio.
select c.nombre as categoria, p.nombre, p.precio as precio_normal, p.precio_promo as precio_lanzamiento, p.proximamente, p.agotado
from productos p join categorias c on c.id = p.categoria_id
order by c.orden, p.orden;

-- =====================================================================
-- MÁS ADELANTE
-- Terminó la semana de lanzamiento: en el panel, "Volver a precios normales". No hace falta tocar la base.
-- Salen los tacos (cargá antes sus precios reales en la columna precio):
--   update productos set proximamente = false where categoria_id = 'tacos';
-- =====================================================================
