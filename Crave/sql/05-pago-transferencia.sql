-- Confirmación de pago por transferencia (punto 5.1).
-- Corré este script en el SQL Editor de Supabase ANTES de subir el código nuevo: el programa nuevo
-- lee estas columnas, y si todavía no existen, el panel y la tienda dan error.
-- Se puede correr más de una vez sin problema: no pisa lo que ya tengas.

-- 1) Cuándo el dueño marcó "transferencia recibida" en el panel.
--    Vacío (null) = todavía no se confirmó. Los pedidos en efectivo siempre quedan vacíos: se cobran al entregar.
alter table pedidos
  add column if not exists pago_confirmado_en timestamptz;

-- 2) Cuántos minutos puede esperar una transferencia sin confirmarse antes de marcarse "Pago demorado" en el panel.
--    Se cambia desde el panel, pestaña "Horarios". No cancela nada solo: solo resalta el pedido.
alter table configuracion
  add column if not exists minutos_pago_demorado int not null default 20
    check (minutos_pago_demorado between 5 and 240);

-- Control: tiene que mostrar 20 en la columna minutos_pago_demorado
select id, minutos_pago_demorado from configuracion;

-- Control: tiene que mostrar la columna pago_confirmado_en (vacía en los pedidos que ya había)
select numero, pago, estado, pago_confirmado_en from pedidos order by id desc limit 5;
