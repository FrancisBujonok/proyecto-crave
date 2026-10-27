-- Configuración del local: interruptor "local cerrado", horarios, cupo y tiempo de preparación.
-- Es una tabla con UNA sola fila (id = 1) que se edita desde el panel (pestaña "Horarios").
-- Se puede correr más de una vez sin problema: no pisa lo que ya configuraste.

create table if not exists configuracion (
  id                   int primary key check (id = 1),   -- el check obliga a que haya una sola fila
  local_abierto        boolean not null default true,
  mensaje_cerrado      text,                              -- lo que ve el cliente cuando está cerrado (puede quedar vacío)
  apertura             text    not null default '20:00',  -- primer horario que se ofrece, formato HH:mm
  ultima_franja        text    not null default '22:30',  -- último horario de entrega que se ofrece
  minutos_preparacion  int     not null default 30 check (minutos_preparacion between 0 and 240),
  minutos_por_franja   int     not null default 30 check (minutos_por_franja between 5 and 120),
  pedidos_por_franja   int     not null default 5  check (pedidos_por_franja between 1 and 50)
);

-- La fila inicial, con los mismos valores por defecto que Models/Configuracion.cs
insert into configuracion (id) values (1) on conflict (id) do nothing;

-- Igual que las demás tablas: RLS activado y sin políticas (solo el servidor la lee y la escribe)
alter table configuracion enable row level security;

-- Control: tiene que mostrar 1 fila
select * from configuracion;
