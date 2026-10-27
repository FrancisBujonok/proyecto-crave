-- =====================================================================
-- CRAVE - envíos por barrio y horarios cada 30 minutos (datos del cliente del 27/10/2026)
-- Pegá este archivo entero en el SQL Editor de Supabase y tocá "Run". Se puede correr más de una vez.
--
-- Qué hace:
--   - Reemplaza las zonas de prueba (Zona 1, 2, 3) por los barrios reales, cada uno con su costo de envío.
--     No hay retiro en el local: todos los pedidos son con envío.
--   - Deja los horarios de 20:00 a 22:30, uno cada 30 minutos, con lugar para 5 pedidos en cada uno
--     (6 horarios x 5 = 30 pedidos por día como máximo). Después se pueden cambiar desde el panel, pestaña "Horarios".
--   - No toca los pedidos: cada pedido ya guardó el nombre del barrio dentro de su dirección.
-- No importa si lo corrés antes o después de subir el código.
-- =====================================================================

begin;

-- ---------- 1) Barrios y costo de envío ----------
-- La página los muestra ordenados por costo y después por nombre.
delete from zonas;

insert into zonas (id, nombre, costo) values
  -- Zonas cercanas
  ('colon',            'Colón',            1000),
  ('9-de-julio',       '9 de Julio',       1000),
  ('villa-del-parque', 'Villa del Parque', 1000),
  -- Distancia media
  ('centro',           'Centro',           1500),
  ('barrio-sancor',    'Barrio Sancor',    1500),
  -- Lejos
  ('moreno',           'Moreno',           2000),
  ('cooperativo',      'Cooperativo',      2000),
  ('zona-sur',         'Zona Sur',         2000),
  ('ciudad-verde',     'Ciudad Verde',     2000),
  ('villa-autodromo',  'Villa Autódromo',  2000);

-- ---------- 2) Horarios ----------
insert into configuracion (id) values (1) on conflict (id) do nothing;   -- por si la fila todavía no existe

update configuracion
set apertura = '20:00',          -- primer horario
    ultima_franja = '22:30',     -- último horario
    minutos_por_franja = 30,     -- uno cada 30 minutos: 20:00, 20:30, 21:00, 21:30, 22:00, 22:30
    pedidos_por_franja = 5       -- lugar para 5 pedidos en cada horario
where id = 1;

commit;

-- ---------- 3) Control ----------
-- Supabase muestra solo el resultado de la ÚLTIMA consulta. Tienen que ser 10 filas (una por barrio),
-- y en todas: 20:00, 22:30, 30 y 5.
select z.nombre as barrio, z.costo as envio,
       c.apertura, c.ultima_franja, c.minutos_por_franja, c.pedidos_por_franja
from zonas z cross join configuracion c
order by z.costo, z.nombre;
