-- =====================================================================
-- CRAVE - salsas a elección dentro de los combos
--
-- ORDEN (importante):
--   1) Pegá este archivo entero en el SQL Editor de Supabase y tocá "Run".
--   2) Recién después subí el código nuevo (commit y push). El programa nuevo lee la tabla "salsas"
--      y la columna "salsas_a_elegir": si todavía no existen, la carta no carga.
--
-- Qué hace:
--   - Crea la tabla "salsas" con los 4 sabores. No son productos: no se venden sueltas ni aparecen en la carta,
--     solo en la ventana que se abre al tocar "+" en un combo (junto con las gaseosas).
--   - Agrega a productos la columna "salsas_a_elegir": cuántas salsas elige el cliente (0 = ninguna).
--   - Una salsa por persona, igual que las gaseosas: cada producto que lleva gaseosas lleva la misma cantidad de salsas
--     (combos, familiares, menú niños y hamburguesas XL). Popcrave, papas, aros, bebidas y tacos no llevan.
--   - No toca precios, pedidos, fotos ni el "agotado" de los productos.
-- Se puede correr más de una vez: no pisa el "agotado" de las salsas. Ojo: vuelve a poner "una salsa por persona"
-- en todos los productos, así que si después cambiaste a mano cuántas salsas lleva alguno, hay que repetir ese cambio.
-- =====================================================================

-- ---------- 1) Tabla de salsas ----------
create table if not exists salsas (
  id       text primary key,                 -- "sal-crave", ...
  nombre   text not null,
  agotado  boolean not null default false,   -- se marca desde el panel; deja de ofrecerse en todos los combos
  orden    int not null default 0            -- orden en la ventana de elección
);

-- Igual que las demás tablas: RLS activado y sin políticas (solo el servidor la lee y la escribe)
alter table salsas enable row level security;

-- ---------- 2) Columna nueva en productos ----------
alter table productos add column if not exists salsas_a_elegir int not null default 0;

begin;

-- ---------- 3) Los 4 sabores ----------
-- Si ya existen, se actualizan nombre y orden (el "agotado" queda como estaba)
insert into salsas (id, nombre, orden) values
  ('sal-chickfila', 'Chick-fil-A', 1),
  ('sal-crave',     'CRAVE',       2),
  ('sal-honey',     'Honey gold',  3),
  ('sal-hotmayo',   'Hot mayo',    4)
on conflict (id) do update set nombre = excluded.nombre, orden = excluded.orden;

-- ---------- 4) Una salsa por persona ----------
-- Los productos que llevan gaseosas llevan la misma cantidad de salsas:
-- Combo individual 1, Combo para 2: 2, Combo para 3: 3, Familiar 4/5/6, menú niños 1 y hamburguesas XL 1.
update productos set salsas_a_elegir = gaseosas_a_elegir;

commit;

-- ---------- 5) Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta: los productos que llevan salsa.
-- Tienen que ser 14 filas, con la misma cantidad en las dos columnas.
-- (Las 4 salsas se ven en Table Editor -> salsas.)
select c.nombre as categoria, p.nombre, p.gaseosas_a_elegir, p.salsas_a_elegir
from productos p join categorias c on c.id = p.categoria_id
where p.salsas_a_elegir > 0
order by c.orden, p.orden;

-- =====================================================================
-- MÁS ADELANTE
-- Para que un producto lleve otra cantidad de salsas (por ejemplo, el Popcrave individual con 1):
--   update productos set salsas_a_elegir = 1 where id = 'ind-popcrave';
-- Para cambiar el nombre de una salsa: Table Editor -> salsas -> columna "nombre".
-- =====================================================================
