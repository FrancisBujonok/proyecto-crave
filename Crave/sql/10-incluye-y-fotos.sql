-- =====================================================================
-- CRAVE - qué incluye cada producto, gaseosas en combos y hamburguesas, y foto del Familiar para 6
--
-- ORDEN (importante):
--   1) Pegá este archivo entero en el SQL Editor de Supabase y tocá "Run".
--   2) Recién después subí el código nuevo (commit y push). El programa nuevo lee la columna "incluye":
--      si todavía no existe, la carta no carga.
--
-- Qué hace:
--   - Agrega la columna "incluye": el cartelito "Incluye ..." de cada tarjeta. Vacía = no se muestra nada
--     (popcrave, papas con cheddar, aros, bebidas y tacos).
--   - Combo individual, Combo para 2 y 3 y las hamburguesas XL pasan a llevar gaseosas a elección
--     (una por persona). El cliente las elige al tocar "+", igual que en los familiares y el menú niños.
--   - Carga la foto del Familiar para 6 (la foto tiene que estar subida antes: viaja con el código).
--   - No toca precios, pedidos ni el "agotado" de cada producto.
-- Se puede correr más de una vez.
-- =====================================================================

alter table productos add column if not exists incluye text;

begin;

-- ---------- Lo que incluye cada producto ----------
update productos set incluye = 'papas + gaseosa'      where id = 'ind-pollo4';
update productos set incluye = '2 papas + 2 gaseosas' where id = 'cmp-pollo8';
update productos set incluye = '3 papas + 3 gaseosas' where id = 'cmp-pollo12';
update productos set incluye = '4 papas + 4 gaseosas' where id = 'fam-4';
update productos set incluye = '5 papas + 5 gaseosas' where id = 'fam-5';
update productos set incluye = '6 papas + 6 gaseosas' where id = 'fam-6';
update productos set incluye = 'papas + gaseosa'      where categoria_id = 'ninos';
update productos set incluye = 'gaseosa'              where categoria_id = 'hamburguesas';

-- ---------- Gaseosas a elección (una por persona) ----------
update productos set gaseosas_a_elegir = 1 where id = 'ind-pollo4';
update productos set gaseosas_a_elegir = 2 where id = 'cmp-pollo8';
update productos set gaseosas_a_elegir = 3 where id = 'cmp-pollo12';
update productos set gaseosas_a_elegir = 1 where categoria_id = 'hamburguesas';

-- Descripciones que ahora mencionan la gaseosa
update productos set descripcion = '4 piezas de pollo frito con papas y gaseosa a elección.'          where id = 'ind-pollo4';
update productos set descripcion = '8 piezas de pollo crujiente + 2 papas + 2 gaseosas a elección.'  where id = 'cmp-pollo8';
update productos set descripcion = '12 piezas de pollo crujiente + 3 papas + 3 gaseosas a elección.' where id = 'cmp-pollo12';

-- ---------- Foto del Familiar para 6 ----------
update productos set imagen = 'img/productos/familiar-6.jpg' where id = 'fam-6';

commit;

-- ---------- Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta. Revisá la columna "incluye" y las gaseosas.
select c.nombre as categoria, p.nombre, p.incluye, p.gaseosas_a_elegir, p.agotado, p.imagen
from productos p join categorias c on c.id = p.categoria_id
order by c.orden, p.orden;
