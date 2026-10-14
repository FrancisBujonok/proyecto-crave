-- =====================================================================
-- CRAVE - base de datos (Supabase / PostgreSQL)
-- Pegalo entero en el SQL Editor y tocá "Run".
-- CUIDADO: empieza borrando las tablas para poder correrlo de nuevo mientras
-- están vacías. Cuando ya haya pedidos reales, NO lo vuelvas a correr.
-- =====================================================================

drop table if exists pedido_items;
drop table if exists pedidos;
drop table if exists productos;
drop table if exists zonas;
drop table if exists categorias;

-- ---------- CARTA ----------
create table categorias (
  id      text primary key,            -- "pollo", "tacos", "extras"
  nombre  text not null,
  nota    text,
  icono   text,
  orden   int  not null default 0
);

create table productos (
  id            text primary key,      -- "p2", "t3", ...
  categoria_id  text not null references categorias (id),   -- no deja borrar una categoría con productos
  nombre        text not null,
  descripcion   text not null default '',
  precio        int  not null,         -- pesos enteros
  icono         text,
  imagen        text,
  etiqueta      text,
  agotado       boolean not null default false,
  destacado     boolean not null default false,
  orden         int     not null default 0
);

create table zonas (
  id      text primary key,            -- "z1", "z2", ...
  nombre  text not null,
  costo   int  not null
);

-- ---------- PEDIDOS ----------
create table pedidos (
  id          bigint generated always as identity primary key,   -- numeración automática
  numero      text not null unique,                              -- "CR-1234", el que ve el cliente
  creado_en   timestamptz not null default now(),
  dia         date not null,           -- día de entrega (hora de Argentina). Junto con "hora" sirve para contar el cupo
  hora        text not null,           -- "21:15"
  nombre      text not null,
  telefono    text not null,
  modo        text not null,           -- "delivery" o "retiro"
  direccion   text not null,
  referencias text,
  pago        text not null,           -- "transferencia" o "efectivo"
  abona_con   int  not null default 0,
  subtotal    int  not null,
  envio       int  not null,
  total       int  not null,
  vuelto      int  not null default 0,
  notas       text,
  estado      text not null default 'nuevo'   -- nuevo, preparando, en_camino, entregado, cancelado
);

-- Para contar rápido cuántos pedidos hay en un horario de un día
create index pedidos_dia_hora_idx on pedidos (dia, hora);

create table pedido_items (
  id           bigint generated always as identity primary key,
  pedido_id    bigint not null references pedidos (id) on delete cascade,   -- si se borra el pedido, se borran sus renglones
  producto_id  text,                   -- sin clave foránea a propósito: el pedido viejo no debe romperse si cambia la carta
  nombre       text not null,          -- copia del nombre al momento de comprar
  cantidad     int  not null,
  precio       int  not null           -- copia del precio al momento de comprar
);

-- ---------- SEGURIDAD ----------
-- RLS activado y sin políticas: nadie entra por la API pública de Supabase.
-- Tu servidor C# se conecta directo a Postgres con la contraseña de la base.
alter table categorias   enable row level security;
alter table productos    enable row level security;
alter table zonas        enable row level security;
alter table pedidos      enable row level security;
alter table pedido_items enable row level security;

-- ---------- DATOS INICIALES: la carta ----------
insert into categorias (id, nombre, nota, icono, orden) values
  ('pollo', 'Pollo frito', 'Combos y baldes', 'pollo', 1),
  ('tacos', 'Tacos', 'Por unidad o en combo', 'taco', 2),
  ('extras', 'Extras y bebidas', 'Para acompañar', 'papas', 3);

insert into productos (id, categoria_id, nombre, descripcion, precio, icono, etiqueta, agotado, destacado, orden) values
  ('p2', 'pollo', 'Combo 2 piezas', '2 piezas de pollo frito, papas y una salsa.', 7900, 'pollo', 'Para 1', false, true, 1),
  ('p4', 'pollo', 'Combo 4 piezas', '4 piezas de pollo frito, papas y dos salsas.', 13900, 'pollo', 'Para 2', false, true, 2),
  ('p8', 'pollo', 'Balde 8 piezas', '8 piezas para compartir, papas grandes y tres salsas.', 24500, 'balde', 'Para 4', false, true, 3),
  ('tend', 'pollo', 'Tenders x6', '6 tiras de pechuga crocante con salsa a elección.', 8900, 'pollo', null, true, false, 4),
  ('tp', 'tacos', 'Taco de pollo crispy', 'Pollo frito, repollo y salsa.', 3600, 'taco', null, false, false, 1),
  ('tc', 'tacos', 'Taco de carne', 'Carne, cebolla, cilantro y salsa.', 3800, 'taco', null, false, false, 2),
  ('t3', 'tacos', 'Combo 3 tacos', '3 tacos a elección y papas.', 10900, 'taco', 'Para 1', false, true, 3),
  ('t6', 'tacos', 'Combo 6 tacos', '6 tacos a elección y papas grandes.', 19900, 'taco', 'Para 2', false, true, 4),
  ('pa', 'extras', 'Papas fritas', 'Porción individual.', 3500, 'papas', null, false, false, 1),
  ('sa', 'extras', 'Salsa extra', 'Pedí la que quieras.', 600, 'salsa', null, false, false, 2),
  ('ga', 'extras', 'Gaseosa 500 ml', 'Línea a elección.', 2200, 'gaseosa', null, false, false, 3),
  ('ag', 'extras', 'Agua 500 ml', 'Con o sin gas.', 1800, 'gaseosa', null, false, false, 4);

insert into zonas (id, nombre, costo) values
  ('z1', 'Zona 1', 1000),
  ('z2', 'Zona 2', 1800),
  ('z3', 'Zona 3', 0);   -- CAMBIÁ el 0 por el costo real de tu Zona 3 (y el nombre si le pusiste otro)
