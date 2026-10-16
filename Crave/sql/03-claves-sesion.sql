-- Claves con las que el servidor firma la cookie de sesión del panel de administración.
-- Se guardan en la base para que sobrevivan a los deploys de Render.
-- Se puede correr más de una vez sin problema.

create table if not exists claves_sesion (
  id            serial primary key,
  friendly_name text,
  xml           text
);

-- Igual que las demás tablas: RLS activado y sin políticas, así solo el servidor (que usa la conexión directa) puede leerla.
alter table claves_sesion enable row level security;
