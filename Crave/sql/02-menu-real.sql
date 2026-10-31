-- =====================================================================
-- CRAVE - carta completa (reemplaza TODA la carta)
-- Pegalo entero en el SQL Editor de Supabase y tocá "Run".
--
-- Para el día a día NO hace falta: los cambios se hicieron con los archivos 08 a 12, que actualizan sin borrar.
-- Este archivo sirve para armar la carta desde cero (por ejemplo, en una base nueva) y tiene la carta completa al día.
--
-- Es SEGURO correrlo con pedidos reales: NO toca las tablas pedidos ni pedido_items.
-- Ojo: borra y vuelve a crear los productos, así que se pierden los "agotado" marcados en el panel
-- y las fotos cargadas con sql/07-fotos-faltantes.sql (corré ese archivo de nuevo después).
--
-- Precios: lista del cliente del 27/10/2026. "precio" es el normal y "precio_promo" el de lanzamiento.
-- Incluye lo de los archivos 10, 11 y 12: gaseosas y salsas a elección (una por persona) y descripciones sin la gaseosa.
-- =====================================================================

-- ---------- 1) Columnas nuevas (si ya existen, no pasa nada) ----------
-- Cuántas gaseosas elige el cliente en cada producto (0 = ninguna)
alter table productos     add column if not exists gaseosas_a_elegir int not null default 0;
-- Texto con las gaseosas elegidas en cada renglón del pedido, ej.: "2 × Coca-Cola, 1 × Sprite"
alter table pedido_items  add column if not exists detalle text;
-- Precio de lanzamiento (o de cualquier promo). Vacío = el producto no tiene precio de promo.
-- Solo se cobra cuando la promo está prendida en el panel (columna promo_activa de configuracion).
alter table productos     add column if not exists precio_promo int;
-- true = el producto se ve con la franja "Próximamente" y todavía no se puede pedir (los tacos)
alter table productos     add column if not exists proximamente boolean not null default false;
-- Cuántas salsas elige el cliente (una por persona, como las gaseosas). Las opciones están en la tabla "salsas".
alter table productos     add column if not exists salsas_a_elegir int not null default 0;
-- El interruptor de la promo. Empieza apagado: se prende desde el panel.
-- "if exists": si la tabla configuracion todavía no existe, no da error. En una base nueva, corré sql/04-configuracion.sql ANTES que este archivo.
alter table if exists configuracion add column if not exists promo_activa boolean not null default false;

-- El precio de promo tiene que ser mayor que 0 y menor que el normal. Si alguien carga otra cosa, la base lo rechaza.
alter table productos drop constraint if exists precio_promo_valido;
alter table productos add constraint precio_promo_valido
  check (precio_promo is null or (precio_promo > 0 and precio_promo < precio));

-- ---------- 2) Carta nueva ----------
begin;

-- Primero los productos y después las categorías (los productos dependen de ellas)
delete from productos;
delete from categorias;

insert into categorias (id, nombre, nota, icono, orden) values
  ('individual', 'Individual', 'Para una persona', 'pollo', 1),
  ('compartir', 'Para compartir', 'Para 2 y 3 personas', 'balde', 2),
  ('familiar', 'Familiar', 'Para 4, 5 y 6 personas', 'balde', 3),
  ('ninos', 'Menú niños', 'Combos con papas y gaseosa', 'hamburguesa', 4),
  ('hamburguesas', 'Hamburguesas', 'Medallón de pollo frito', 'hamburguesa', 5),
  ('bebidas', 'Bebidas', 'Gaseosas individuales', 'gaseosa', 6),
  ('tacos', 'Tacos', 'Próximamente', 'taco', 7);

-- Salsas (se eligen dentro de los combos; no aparecen como productos)
create table if not exists salsas (
  id text primary key, nombre text not null, agotado boolean not null default false, orden int not null default 0
);
alter table salsas enable row level security;
delete from salsas;
insert into salsas (id, nombre, orden) values
  ('sal-chickfila', 'Chick-fil-A', 1),
  ('sal-crave',     'CRAVE',       2),
  ('sal-honey',     'Honey gold',  3),
  ('sal-hotmayo',   'Hot mayo',    4);

-- Columnas: id, categoria_id, nombre, descripcion, precio, precio_promo, icono, imagen, etiqueta, destacado, orden, gaseosas_a_elegir, salsas_a_elegir, proximamente
insert into productos (id, categoria_id, nombre, descripcion, precio, precio_promo, icono, imagen, etiqueta, destacado, orden, gaseosas_a_elegir, salsas_a_elegir, proximamente) values
  -- INDIVIDUAL
  ('ind-pollo4', 'individual', 'Combo individual', '4 piezas de pollo frito con papas.', 17900, 14000, 'pollo', 'img/productos/pollo-4-piezas.jpg', 'Para 1', true, 1, 1, 1, false),
  ('ind-popcrave', 'individual', 'Popcrave individual', 'Porción de 10 unidades.', 9000, 7000, 'pollo', 'img/productos/popcrave.jpg', null, false, 2, 0, 0, false),
  ('ind-papas-ch', 'individual', 'Papas con cheddar y bacon', 'Porción individual.', 9500, 8000, 'papas', 'img/productos/papas-cheddar-bacon.jpg', null, false, 3, 0, 0, false),
  ('ind-aros', 'individual', 'Aros de cebolla', 'Porción de 8 unidades.', 6000, 5000, 'papas', 'img/productos/aros-cebolla.jpg', null, false, 4, 0, 0, false),

  -- PARA COMPARTIR
  ('cmp-pollo8', 'compartir', 'Combo para 2', '8 piezas de pollo crujiente + 2 papas.', 35000, 29900, 'balde', 'img/productos/combo-2-personas.jpg', 'Para 2', true, 1, 2, 2, false),
  ('cmp-pollo12', 'compartir', 'Combo para 3', '12 piezas de pollo crujiente + 3 papas.', 49900, 41000, 'balde', 'img/productos/combo-3-personas.jpg', 'Para 3', true, 2, 3, 3, false),

  -- FAMILIAR (cada uno lleva tantas gaseosas a elección como personas)
  ('fam-4', 'familiar', 'Familiar para 4', '16 piezas de pollo crujiente + 4 papas.', 65000, 55000, 'balde', 'img/productos/familiar-4.jpg', 'Para 4', true, 1, 4, 4, false),
  ('fam-5', 'familiar', 'Familiar para 5', '20 piezas de pollo crujiente + 5 papas.', 78000, 65000, 'balde', 'img/productos/familiar-5.jpg', 'Para 5', false, 2, 5, 5, false),
  ('fam-6', 'familiar', 'Familiar para 6', '24 piezas de pollo crujiente + 6 papas.', 92000, 77000, 'balde', 'img/productos/familiar-6.jpg', 'Para 6', false, 3, 6, 6, false),

  -- MENÚ NIÑOS (todos con papas y una gaseosa a elección)
  ('nin-popcrave', 'ninos', 'Combo kids popcrave', 'Popcrave y papas.', 13500, 11000, 'pollo', 'img/productos/combo-ninos-popcrave.jpg', 'Niños', false, 1, 1, 1, false),
  ('nin-simple', 'ninos', 'Combo kids cheddar', 'Hamburguesa con cheddar + papas.', 13900, 10900, 'hamburguesa', 'img/productos/mini-crave-ninos.jpg', 'Niños', false, 2, 1, 1, false),
  ('nin-fresh', 'ninos', 'Combo kids fresh', 'Fresh chicken mediana + papas.', 14500, 11500, 'hamburguesa', 'img/productos/fresh-chicken-ninos.jpg', 'Niños', false, 3, 1, 1, false),
  ('nin-crispy', 'ninos', 'Combo kids crispy club', 'Crispy club mediana + papas.', 14500, 11500, 'hamburguesa', 'img/productos/crispy-club-ninos.jpg', 'Niños', false, 4, 1, 1, false),
  ('nin-sweet', 'ninos', 'Combo kids sweet', 'Sweet crave mediana + papas.', 14000, 11500, 'hamburguesa', 'img/productos/sweet-crave-ninos.jpg', 'Niños', false, 5, 1, 1, false),

  -- HAMBURGUESAS
  ('ham-fresh', 'hamburguesas', 'Fresh chicken XL', 'Medallón de pollo frito, salsa ranch, salsa alioli, lechuga, pepinillos y cheddar.', 17000, 13500, 'hamburguesa', 'img/productos/fresh-chicken-grande.jpg', 'XL', false, 1, 1, 1, false),
  ('ham-sweet', 'hamburguesas', 'Sweet crave XL', 'Medallón de pollo, jalea de barbacoa, cheddar y salsa de la casa.', 18000, 14500, 'hamburguesa', 'img/productos/sweet-crave-grande.jpg', 'XL', false, 2, 1, 1, false),
  ('ham-crispy', 'hamburguesas', 'Crispy club XL', 'Medallón de pollo frito, lechuga, tomate, salsa de la casa, panceta y cheddar.', 17000, 13500, 'hamburguesa', 'img/productos/crispy-club-grande.jpg', 'XL', false, 3, 1, 1, false),

  -- BEBIDAS: también son las gaseosas que se eligen dentro de los combos. Si marcás una como agotada, deja de ofrecerse en todos lados.
  ('beb-coca', 'bebidas', 'Coca-Cola', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-coca.jpg', null, false, 1, 0, 0, false),
  ('beb-sprite', 'bebidas', 'Sprite', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-sprite.jpg', null, false, 2, 0, 0, false),
  ('beb-fanta', 'bebidas', 'Fanta', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-fanta.jpg', null, false, 3, 0, 0, false),
  ('beb-zero', 'bebidas', 'Coca-Cola Zero', 'Gaseosa individual.', 4000, 3500, 'gaseosa', 'img/productos/gaseosa-zero.jpg', null, false, 4, 0, 0, false),

  -- TACOS: se ven con la franja "Próximamente" y todavía no se pueden pedir. Sus precios son provisorios y no se muestran.
  ('tac-pollo', 'tacos', 'Taco de pollo crispy', 'Pollo frito, repollo y salsa.', 3600, null, 'taco', null, null, false, 1, 0, 0, true),
  ('tac-carne', 'tacos', 'Taco de carne', 'Carne, cebolla, cilantro y salsa.', 3800, null, 'taco', null, null, false, 2, 0, 0, true),
  ('tac-combo3', 'tacos', 'Combo 3 tacos', '3 tacos a elección y papas.', 10900, null, 'taco', null, 'Para 1', false, 3, 0, 0, true),
  ('tac-combo6', 'tacos', 'Combo 6 tacos', '6 tacos a elección y papas grandes.', 19900, null, 'taco', null, 'Para 2', false, 4, 0, 0, true);

commit;

-- ---------- 3) Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta. Arriba de la tabla tiene que decir 25 filas
-- (21 con precio de lanzamiento y 4 tacos con proximamente = true). Revisalos precio por precio.
select c.nombre as categoria, p.nombre, p.precio as precio_normal, p.precio_promo as precio_lanzamiento, p.gaseosas_a_elegir, p.salsas_a_elegir, p.proximamente, p.agotado
from productos p join categorias c on c.id = p.categoria_id
order by c.orden, p.orden;
