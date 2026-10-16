-- Seguimiento del pedido para el cliente (punto 5.2).
-- Corré este script en el SQL Editor de Supabase ANTES de subir el código nuevo: el programa nuevo
-- lee y guarda esta columna, y si todavía no existe, la tienda y el panel dan error.
-- Se puede correr más de una vez sin problema: no pisa lo que ya tengas.

-- Código largo y al azar con el que el cliente mira el estado de SU pedido (ej.: K7M2QX9TRB4H).
-- Va en el enlace /seguimiento.html?c=CODIGO. No se usa el número "CR-1001" porque es correlativo:
-- cualquiera podría probar CR-1002, CR-1003... y mirar los pedidos de otros.
-- Los pedidos anteriores a este cambio quedan sin código (vacío): no tienen página de seguimiento.
alter table pedidos
  add column if not exists codigo_seguimiento text;

-- No puede haber dos pedidos con el mismo código, y la búsqueda por código tiene que ser rápida.
-- (Los pedidos sin código no cuentan: por eso el "where".)
create unique index if not exists pedidos_codigo_seguimiento_idx
  on pedidos (codigo_seguimiento)
  where codigo_seguimiento is not null;

-- Control: tiene que mostrar la columna codigo_seguimiento (vacía en los pedidos que ya había)
select numero, estado, codigo_seguimiento from pedidos order by id desc limit 5;
