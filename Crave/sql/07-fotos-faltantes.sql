-- =====================================================================
-- CRAVE - fotos de los productos que todavía no tienen
--
-- Orden: 1) copiá las fotos en wwwroot/img/productos/ con EXACTAMENTE estos nombres,
--        2) subí el código con las fotos (commit y push),
--        3) recién entonces corré las líneas de abajo en el SQL Editor de Supabase.
--
-- Corré solo las líneas de las fotos que ya copiaste (borrá o dejá comentadas las otras).
-- Si alguna foto todavía no existe, la tienda muestra el dibujo del producto en su lugar,
-- así que no se rompe nada, pero el navegador hace un pedido de más.
--
-- No toca pedidos ni precios: solo la columna "imagen" de productos.
-- Es seguro correrlo más de una vez.
--
-- Si algún día volvés a correr sql/02-menu-real.sql (reemplaza la carta completa),
-- las fotos de este archivo se pierden: corré este archivo de nuevo después.
-- =====================================================================

-- ---------- Alitas (los tres productos de alitas) ----------
update productos set imagen = 'img/productos/alitas-individual.jpg' where id = 'ind-alitas6';    -- 6 alitas + papas
update productos set imagen = 'img/productos/alitas-2-personas.jpg' where id = 'cmp-alitas10';   -- Para 2: alitas
update productos set imagen = 'img/productos/alitas-3-personas.jpg' where id = 'cmp-alitas15';   -- Para 3: alitas

-- ---------- Familiar para 6 ----------
update productos set imagen = 'img/productos/familiar-6.jpg' where id = 'fam-6';

-- ---------- Tacos (hoy aparecen como agotados; las fotos pueden esperar al lanzamiento) ----------
-- update productos set imagen = 'img/productos/taco-pollo.jpg'   where id = 'tac-pollo';
-- update productos set imagen = 'img/productos/taco-carne.jpg'   where id = 'tac-carne';
-- update productos set imagen = 'img/productos/combo-3-tacos.jpg' where id = 'tac-combo3';
-- update productos set imagen = 'img/productos/combo-6-tacos.jpg' where id = 'tac-combo6';

-- ---------- Control: los productos que siguen sin foto (si no sale ninguno, ya está todo) ----------
select id, nombre, categoria_id from productos where imagen is null or imagen = '' order by categoria_id, orden;
