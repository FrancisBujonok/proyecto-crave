-- =====================================================================
-- CRAVE - carta real + gaseosas a elección
-- Pegalo entero en el SQL Editor de Supabase y tocá "Run".
--
-- Es SEGURO correrlo con pedidos reales: NO toca las tablas pedidos ni pedido_items.
-- Se puede correr de nuevo las veces que haga falta: reemplaza la carta completa.
--
-- !!! LOS PRECIOS SON PROVISORIOS !!!
-- Cuando tengas los reales, cambialos en Table Editor -> productos -> columna "precio"
-- (o editá este archivo y volvé a correrlo).
-- =====================================================================

-- ---------- 1) Columnas nuevas (si ya existen, no pasa nada) ----------
-- Cuántas gaseosas elige el cliente en cada producto (0 = ninguna)
alter table productos    add column if not exists gaseosas_a_elegir int not null default 0;
-- Texto con las gaseosas elegidas en cada renglón del pedido, ej.: "2 × Coca-Cola, 1 × Sprite"
alter table pedido_items add column if not exists detalle text;

-- ---------- 2) Carta nueva ----------
begin;

-- Primero los productos y después las categorías (los productos dependen de ellas)
delete from productos;
delete from categorias;

insert into categorias (id, nombre, nota, icono, orden) values
  ('individual',   'Individual',      'Para una persona',      'pollo',       1),
  ('compartir',    'Para compartir',  'Para 2 y 3 personas',   'balde',       2),
  ('familiar',     'Familiar',        'Para 4, 5 y 6 personas','balde',       3),
  ('ninos',        'Menú niños',      'Porciones chicas',      'hamburguesa', 4),
  ('hamburguesas', 'Hamburguesas',    'Medallón de pollo frito','hamburguesa',5),
  ('bebidas',      'Bebidas',         'Gaseosas individuales', 'gaseosa',     6),
  ('tacos',        'Tacos',           'Próximamente',          'taco',        7);

-- Columnas: id, categoria_id, nombre, descripcion, precio, icono, imagen, etiqueta, agotado, destacado, orden, gaseosas_a_elegir
insert into productos (id, categoria_id, nombre, descripcion, precio, icono, imagen, etiqueta, agotado, destacado, orden, gaseosas_a_elegir) values

  -- INDIVIDUAL
  ('ind-popcrave',  'individual', 'Popcrave',                    'Porción de 10 unidades.',                                  6500,  'pollo', 'img/productos/popcrave.jpg',            null,     false, false, 1, 0),
  ('ind-aros',      'individual', 'Aros de cebolla',             'Porción de 8 unidades.',                                   5500,  'papas', 'img/productos/aros-cebolla.jpg',        null,     false, false, 2, 0),
  ('ind-pollo4',    'individual', '4 piezas de pollo + papas',   '4 piezas de pollo frito con papas individuales.',          11500, 'pollo', 'img/productos/pollo-4-piezas.jpg',      'Para 1', false, true,  3, 0),
  ('ind-alitas6',   'individual', '6 alitas + papas',            '6 piezas de alitas con papas individuales.',               10500, 'pollo', null,                                    'Para 1', false, false, 4, 0),
  ('ind-papas-ch',  'individual', 'Papas con cheddar y bacon',   'Porción individual.',                                      7500,  'papas', 'img/productos/papas-cheddar-bacon.jpg', null,     false, false, 5, 0),

  -- PARA COMPARTIR
  ('cmp-pollo8',    'compartir',  'Para 2: pollo crujiente',     '8 piezas de pollo crujiente + 2 papas.',                   21000, 'balde', 'img/productos/combo-2-personas.jpg',    'Para 2', false, true,  1, 0),
  ('cmp-alitas10',  'compartir',  'Para 2: alitas',              '10 alitas de pollo crujiente + 2 papas + 2 salsas.',       19500, 'balde', null,                                    'Para 2', false, false, 2, 0),
  ('cmp-pollo12',   'compartir',  'Para 3: pollo crujiente',     '12 piezas de pollo crujiente + 3 papas.',                  30500, 'balde', 'img/productos/combo-3-personas.jpg',    'Para 3', false, true,  3, 0),
  ('cmp-alitas15',  'compartir',  'Para 3: alitas',              '15 alitas crujientes + 3 papas.',                          28500, 'balde', null,                                    'Para 3', false, false, 4, 0),

  -- FAMILIAR (cada uno lleva tantas gaseosas a elección como personas)
  ('fam-4',         'familiar',   'Familiar para 4',             '16 piezas de pollo crujiente + 4 papas + 4 gaseosas a elección.', 46000, 'balde', 'img/productos/familiar-4.jpg', 'Para 4', false, true,  1, 4),
  ('fam-5',         'familiar',   'Familiar para 5',             '20 piezas de pollo crujiente + 5 papas + 5 gaseosas a elección.', 56500, 'balde', 'img/productos/familiar-5.jpg', 'Para 5', false, false, 2, 5),
  ('fam-6',         'familiar',   'Familiar para 6',             '24 piezas de pollo crujiente + 6 papas + 6 gaseosas a elección.', 67000, 'balde', null,                          'Para 6', false, false, 3, 6),

  -- MENÚ NIÑOS
  ('nin-simple',    'ninos',      'Hamburguesa simple',          'Medallón de pollo, queso cheddar y salsa de la casa.',     5500,  'hamburguesa', 'img/productos/mini-crave-ninos.jpg',      'Niños', false, false, 1, 0),
  ('nin-fresh',     'ninos',      'Fresh chicken mediana',       'Hamburguesa mediana + papas + gaseosa a elección.',        9500,  'hamburguesa', 'img/productos/fresh-chicken-ninos.jpg',   'Niños', false, false, 2, 1),
  ('nin-sweet',     'ninos',      'Sweet crave mediana',         'Hamburguesa mediana + papas + gaseosa a elección.',        9800,  'hamburguesa', 'img/productos/sweet-crave-ninos.jpg',     'Niños', false, false, 3, 1),
  ('nin-crispy',    'ninos',      'Crispy club mediana',         'Hamburguesa mediana + papas + gaseosa a elección.',        10200, 'hamburguesa', 'img/productos/crispy-club-ninos.jpg',     'Niños', false, false, 4, 1),
  ('nin-popcrave',  'ninos',      'Popcrave + papas + gaseosa',  'Popcrave, papas y gaseosa chica a elección.',              9000,  'pollo',       'img/productos/combo-ninos-popcrave.jpg',  'Niños', false, false, 5, 1),

  -- HAMBURGUESAS (adultos)
  ('ham-fresh',     'hamburguesas', 'Fresh chicken grande',      'Medallón de pollo frito, salsa ranch, salsa alioli, lechuga, pepinillos y cheddar.', 9800,  'hamburguesa', 'img/productos/fresh-chicken-grande.jpg', 'Grande', false, false, 1, 0),
  ('ham-sweet',     'hamburguesas', 'Sweet crave grande',        'Medallón de pollo, jalea de barbacoa, cheddar y salsa de la casa.',                  10200, 'hamburguesa', 'img/productos/sweet-crave-grande.jpg', 'Grande', false, false, 2, 0),
  ('ham-crispy',    'hamburguesas', 'Crispy club grande',        'Medallón de pollo frito, lechuga, tomate, salsa de la casa, panceta y cheddar.',     10800, 'hamburguesa', 'img/productos/crispy-club-grande.jpg', 'Grande', false, false, 3, 0),

  -- BEBIDAS: estas cuatro son las gaseosas que se ofrecen dentro de los combos.
  -- Si marcás una como agotada, deja de ofrecerse en todos lados.
  ('beb-coca',      'bebidas', 'Coca-Cola',       'Gaseosa individual.', 2500, 'gaseosa', 'img/productos/gaseosa-coca.jpg',   null, false, false, 1, 0),
  ('beb-sprite',    'bebidas', 'Sprite',          'Gaseosa individual.', 2500, 'gaseosa', 'img/productos/gaseosa-sprite.jpg', null, false, false, 2, 0),
  ('beb-fanta',     'bebidas', 'Fanta',           'Gaseosa individual.', 2500, 'gaseosa', 'img/productos/gaseosa-fanta.jpg',  null, false, false, 3, 0),
  ('beb-zero',      'bebidas', 'Coca-Cola Zero',  'Gaseosa individual.', 2500, 'gaseosa', 'img/productos/gaseosa-zero.jpg',   null, false, false, 4, 0),

  -- TACOS: aparecen en la carta pero todavía sin stock (agotado = true)
  ('tac-pollo',     'tacos', 'Taco de pollo crispy', 'Pollo frito, repollo y salsa.',        3600,  'taco', null, null,     true, false, 1, 0),
  ('tac-carne',     'tacos', 'Taco de carne',        'Carne, cebolla, cilantro y salsa.',    3800,  'taco', null, null,     true, false, 2, 0),
  ('tac-combo3',    'tacos', 'Combo 3 tacos',        '3 tacos a elección y papas.',          10900, 'taco', null, 'Para 1', true, false, 3, 0),
  ('tac-combo6',    'tacos', 'Combo 6 tacos',        '6 tacos a elección y papas grandes.',  19900, 'taco', null, 'Para 2', true, false, 4, 0);

commit;

-- ---------- 3) Control: tiene que mostrar 7 categorías y 28 productos ----------
select (select count(*) from categorias) as categorias,
       (select count(*) from productos)  as productos;
