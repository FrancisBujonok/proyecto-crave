-- =====================================================================
-- CRAVE - descripciones sin la gaseosa (ahora la muestra el cartelito "Incluye gaseosa")
--
-- ORDEN (importante):
--   1) Primero subí el código nuevo (commit y push) y esperá a que Render diga "Live".
--   2) Recién después pegá este archivo en el SQL Editor de Supabase y tocá "Run".
--   (Si lo corrés antes, el código viejo busca la columna "incluye" que este archivo borra, y la carta no carga.)
--
-- Qué hace:
--   - Saca "+ gaseosas a elección" de las descripciones: el cartelito ya lo dice, sale de gaseosas_a_elegir.
--   - Borra la columna "incluye", que ya no se usa.
--   - No toca precios, pedidos, fotos ni gaseosas_a_elegir.
-- Se puede correr más de una vez.
-- =====================================================================

begin;

update productos set descripcion = '4 piezas de pollo frito con papas.'       where id = 'ind-pollo4';
update productos set descripcion = '8 piezas de pollo crujiente + 2 papas.'   where id = 'cmp-pollo8';
update productos set descripcion = '12 piezas de pollo crujiente + 3 papas.'  where id = 'cmp-pollo12';
update productos set descripcion = '16 piezas de pollo crujiente + 4 papas.'  where id = 'fam-4';
update productos set descripcion = '20 piezas de pollo crujiente + 5 papas.'  where id = 'fam-5';
update productos set descripcion = '24 piezas de pollo crujiente + 6 papas.'  where id = 'fam-6';
update productos set descripcion = 'Popcrave y papas.'                        where id = 'nin-popcrave';
update productos set descripcion = 'Hamburguesa con cheddar + papas.'         where id = 'nin-simple';
update productos set descripcion = 'Fresh chicken mediana + papas.'           where id = 'nin-fresh';
update productos set descripcion = 'Crispy club mediana + papas.'             where id = 'nin-crispy';
update productos set descripcion = 'Sweet crave mediana + papas.'             where id = 'nin-sweet';

alter table productos drop column if exists incluye;

commit;

-- ---------- Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta. Las que tienen gaseosas muestran el cartelito en la página.
select c.nombre as categoria, p.nombre, p.descripcion, p.gaseosas_a_elegir
from productos p join categorias c on c.id = p.categoria_id
order by c.orden, p.orden;
