/* =====================================================================
   CRAVE - lógica de la página
   Leelo de arriba hacia abajo:
   1 Configuración   2 Estado   3 Ayudantes   4 Dibujar la página   5 Carrito
   6 Formulario y horarios   7 Cajón   8 Confirmación   9 Animaciones   10 Arranque
   ===================================================================== */

// ---------- 1. CONFIGURACIÓN ----------
// true  = usa la carta de menu-demo.js y simula el envío (sirve para ver la página sin servidor).
// false = pide la carta a /api/menu y manda el pedido a /api/pedidos (servidor C#).
const MODO_DEMO = false;
const paginaAbiertaEn = Date.now();   // para el antispam: el servidor mira cuánto tiempo estuvo abierta la página

const CONFIG = {
  whatsapp: "5490000000000",          // ejemplo: reemplazar por el número real. Formato: 54 + 9 + código de área sin 0 + número sin 15 (ej.: 5493493123456)
  alias: "CRAVE.PEDIDOS.EJEMPLO",
  titular: "Titular de ejemplo"
};

// Solo para el modo demo: imita lo que hace HorarioService.cs en el servidor.
// En el modo real estos números se configuran en C# y esta constante no se usa.
const DEMO_HORARIOS = { apertura: "20:00", ultima: "23:30", cadaMinutos: 15, cupo: 4, ahora: "19:40", preparacion: 30 };

// ---------- 2. ESTADO (los datos que cambian) ----------
let menu = { categorias: [], zonas: [] };
let productos = {};      // id -> producto, para encontrarlo rápido
const MAXIMO_POR_PRODUCTO = 30;   // el servidor también lo exige (PedidoService.Validar)
let carrito = {};        // productos comunes: id -> cantidad. Ejemplo: { "ind-pollo4": 2, "beb-coca": 1 }
// Productos con gaseosas a elegir (ej.: Familiar para 4): un renglón por cada unidad, porque cada una
// puede llevar gaseosas distintas. Ejemplo: [{ uid: 1, id: "fam-4", gaseosas: ["beb-coca", "beb-coca", "beb-fanta", "beb-sprite"] }]
let combos = [];
let siguienteUid = 1;    // numerador para identificar cada renglón de "combos"
let selector = null;     // lo que se está eligiendo en la ventana de gaseosas: { id, uid, origen, cantidades }
let franjas = [];        // horarios de hoy: [{ hora: "21:15", libres: 3 }, ...]
let franjasError = "";  // texto para mostrar si no hay horarios o no se pudieron cargar
let localCerrado = false;  // true cuando el dueño cerró el local desde el panel
let quienAbrio = null;   // botón que abrió el cajón, para devolverle el foco al cerrar

// ---------- 3. AYUDANTES ----------
const $ = (selector, dentro = document) => dentro.querySelector(selector);
const $$ = (selector, dentro = document) => [...dentro.querySelectorAll(selector)];
const dinero = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS", maximumFractionDigits: 0 });
const sinMovimiento = window.matchMedia("(prefers-reduced-motion: reduce)");

// Convierte texto en algo seguro para meter dentro de HTML
function esc(texto) {
  return String(texto ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

// Dibuja un icono de los que están definidos arriba en index.html
function icono(nombre) {
  return '<svg class="icono" viewBox="0 0 64 64" aria-hidden="true"><use href="#i-' + nombre + '"/></svg>';
}
function iconoChico(nombre) {
  return '<svg class="icono" viewBox="0 0 24 24" aria-hidden="true"><use href="#i-' + nombre + '"/></svg>';
}

// Avisa a lectores de pantalla (no se ve en pantalla)
function anunciar(texto) { $("#aviso").textContent = texto; }

// ¿Este producto lleva gaseosas a elegir? (lo define la columna gaseosas_a_elegir de la base)
function tieneOpciones(p) { return (p.gaseosasAElegir || 0) > 0; }

// Las gaseosas que se pueden elegir hoy: los productos de la categoría "bebidas" que no están agotados
function gaseosasDisponibles() {
  const bebidas = menu.categorias.find(c => c.id === "bebidas");
  return bebidas ? bebidas.productos.filter(p => !p.agotado) : [];
}

// [coca, coca, sprite] -> "2 × Coca-Cola, 1 × Sprite". Con una sola gaseosa: solo el nombre.
function textoGaseosas(ids) {
  if (ids.length === 1) return productos[ids[0]].nombre;
  const cuenta = {};
  ids.forEach(id => { cuenta[id] = (cuenta[id] || 0) + 1; });
  return Object.keys(cuenta).map(id => cuenta[id] + " × " + productos[id].nombre).join(", ");
}

// Cuántas unidades de un producto hay en el pedido (sea común o con gaseosas)
function cantidadDe(id) {
  if (tieneOpciones(productos[id])) return combos.filter(c => c.id === id).length;
  return carrito[id] || 0;
}

// ---------- 4. DIBUJAR LA PÁGINA ----------
async function cargarMenu() {
  if (MODO_DEMO) return MENU_DEMO;
  const respuesta = await fetch("/api/menu");
  if (!respuesta.ok) throw new Error("No se pudo cargar la carta");
  return await respuesta.json();
}

// Filas inclinadas de "CRAVE" detrás de la portada
function dibujarFilas() {
  const grupo = '<span class="grupo">' + "CRAVE ".repeat(8) + "</span>";
  let html = "";
  for (let i = 0; i < 5; i++) {
    html += '<div class="fila" style="animation-delay:-' + i * 9 + 's">' + grupo + grupo + "</div>";
  }
  $("#filas").innerHTML = html;
}

// Cinta blanca: el mismo grupo dos veces para que el loop no tenga saltos
function dibujarCinta() {
  const frases = ["Pollo frito crocante", "Tacos", "Delivery en Sunchales", "Transferencia o efectivo", "Feed the craving"];
  let grupo = '<div class="cinta-grupo">';
  frases.concat(frases).forEach(frase => {
    grupo += "<span>" + frase + '</span><svg class="chev" aria-hidden="true"><use href="#i-chevron"/></svg>';
  });
  grupo += "</div>";
  $("#cinta").innerHTML = grupo + grupo;
}

function dibujarPastillas() {
  let html = '<a class="pastilla" href="#combos" data-cat="combos" aria-current="true">Combos</a>';
  menu.categorias.forEach(c => {
    html += '<a class="pastilla" href="#cat-' + c.id + '" data-cat="' + c.id + '" aria-current="false">' + esc(c.nombre) + "</a>";
  });
  $("#pastillas").innerHTML = html;
}

function dibujarAntojos() {
  $("#antojos").innerHTML = menu.categorias.map(c => `
    <a class="antojo" href="#cat-${c.id}">
      ${icono(c.icono)}
      <span><b>${esc(c.nombre)}</b><small>${esc(c.nota)}</small></span>
    </a>`).join("");
}

// La tarjeta de un producto. Se usa en los combos y en la carta.
function tarjetaHTML(p) {
  const arte = p.imagen
    ? '<img class="foto" src="' + esc(p.imagen) + '" data-icono="' + esc(p.icono || "") + '" alt="' + esc(p.nombre) + '" width="400" height="300" loading="lazy">'
    : icono(p.icono);
  const etiqueta = p.etiqueta ? '<span class="etiqueta">' + esc(p.etiqueta) + "</span>" : "";
  return `
    <li class="reveal">
      <article class="tarjeta ${p.agotado ? "agotado" : ""}">
        <div class="arte">${arte}${etiqueta}</div>
        <div class="cuerpo-tarjeta">
          <h3 class="nombre">${esc(p.nombre)}</h3>
          <p class="desc">${esc(p.descripcion)}</p>
          <div class="pie-tarjeta">
            <span class="precio">${dinero.format(p.precio)}</span>
            <div class="control" data-id="${p.id}">${controlHTML(p.id)}</div>
          </div>
        </div>
      </article>
    </li>`;
}

// El botón "+" o, si ya hay unidades en el pedido, el selector de cantidad
function controlHTML(id) {
  const p = productos[id];
  const cantidad = cantidadDe(id);
  if (p.agotado) return '<span class="chip-agotado">Agotado</span>';
  if (cantidad === 0) {
    return '<button type="button" class="mas" data-accion="sumar" data-id="' + id + '" aria-label="Agregar ' + esc(p.nombre) + '">' + iconoChico("mas") + "</button>";
  }
  return '<div class="cantidad" role="group" aria-label="Cantidad de ' + esc(p.nombre) + '">'
    + '<button type="button" data-accion="restar" data-id="' + id + '" aria-label="Quitar una unidad de ' + esc(p.nombre) + '">' + iconoChico("menos") + "</button>"
    + "<output>" + cantidad + "</output>"
    + '<button type="button" data-accion="sumar" data-id="' + id + '" aria-label="Agregar una unidad de ' + esc(p.nombre) + '">' + iconoChico("mas") + "</button>"
    + "</div>";
}

function dibujarCombos() {
  const lista = [];
  menu.categorias.forEach(c => c.productos.forEach(p => { if (p.destacado) lista.push(p); }));
  $("#carril").innerHTML = lista.map(tarjetaHTML).join("");
}

function dibujarCarta() {
  $("#menu").innerHTML = menu.categorias.map(c => `
    <section class="seccion categoria" id="cat-${c.id}" data-cat="${c.id}" aria-labelledby="t-${c.id}">
      <div class="titulo-fila">
        <h2 class="titulo" id="t-${c.id}">${esc(c.nombre)}</h2>
        <p class="nota">${esc(c.nota)}</p>
      </div>
      <ul class="grilla">${c.productos.map(tarjetaHTML).join("")}</ul>
    </section>`).join("");
}

// ----- Horarios con cupo -----
// "20:15" -> minutos desde medianoche (1215). Sirve para sumar y comparar horas.
function aMinutos(texto) { const [h, m] = texto.split(":"); return Number(h) * 60 + Number(m); }
function aTexto(minutos) { return String(Math.floor(minutos / 60)).padStart(2, "0") + ":" + String(minutos % 60).padStart(2, "0"); }

// Modo demo: arma los horarios como lo haría el servidor. Algunos ya vienen llenos para mostrar cómo se ven.
let ocupadosDemo = { "20:15": 3, "20:30": 4, "21:00": 2, "21:30": 4 };   // hora -> pedidos ya tomados
function franjasDeEjemplo() {
  const d = DEMO_HORARIOS;
  const minimo = aMinutos(d.ahora) + d.preparacion;
  const lista = [];
  for (let t = aMinutos(d.apertura); t <= aMinutos(d.ultima); t += d.cadaMinutos) {
    if (t < minimo) continue;
    const hora = aTexto(t);
    lista.push({ hora: hora, libres: Math.max(d.cupo - (ocupadosDemo[hora] || 0), 0) });
  }
  return lista;
}

async function cargarFranjas() {
  if (MODO_DEMO) { franjas = franjasDeEjemplo(); franjasError = ""; localCerrado = false; return; }
  try {
    const respuesta = await fetch("/api/franjas");
    if (!respuesta.ok) throw new Error("franjas");
    const datos = await respuesta.json();
    franjas = datos.franjas;
    franjasError = datos.aviso;          // el servidor manda un texto para usar si no queda ningún lugar
    localCerrado = datos.cerrado === true;
  } catch (e) {
    franjas = [];
    franjasError = "No pudimos cargar los horarios.";
  }
}

// El horario marcado ("" si todavía no hay botones de horario o ninguno está elegido)
function horaElegida() {
  const marcado = $('input[name="hora"]:checked');
  return marcado ? marcado.value : "";
}

function hayHorario() { return franjas.some(f => f.libres > 0); }

// Texto chico debajo de cada hora
function textoCupo(libres) {
  if (libres <= 0) return "Completo";
  if (libres === 1) return "Último lugar";
  if (libres === 2) return "Quedan 2";
  return "Hay lugar";
}

// Dibuja los botones de horario. Mantiene la elección anterior si todavía tiene lugar;
// si no, deja marcado el primero disponible.
function dibujarFranjas() {
  const anterior = horaElegida();
  const elegido = franjas.some(f => f.hora === anterior && f.libres > 0)
    ? anterior
    : (franjas.find(f => f.libres > 0) || {}).hora;

  $("#f-hora").innerHTML = franjas.map(f => {
    const completa = f.libres <= 0;
    const clases = "franja" + (completa ? " completa" : "") + (f.libres === 1 ? " ultimo" : "");
    return '<label class="' + clases + '"><input type="radio" name="hora" value="' + f.hora + '"'
      + (completa ? " disabled" : "") + (f.hora === elegido ? " checked" : "") + ">"
      + "<b>" + f.hora + "</b><small>" + textoCupo(f.libres) + "</small></label>";
  }).join("");

  // Cartel de "local cerrado" arriba de todo, para que el cliente no arme el pedido en vano
  const banner = $("#banner-cerrado");
  banner.hidden = !localCerrado;
  banner.textContent = localCerrado ? franjasError : "";

  const aviso = $("#aviso-franjas");
  aviso.hidden = hayHorario();
  if (!hayHorario()) {
    aviso.innerHTML = esc(franjasError || "Por hoy no quedan horarios disponibles.")
      + '<br><button type="button" class="copiar" data-reintentar="franjas">Volver a mirar</button>';
  }
  actualizarBotonConfirmar();
}

function actualizarBotonConfirmar() {
  $("#boton-confirmar").disabled = !hayHorario();
}

async function refrescarFranjas() {
  await cargarFranjas();
  dibujarFranjas();
}

function llenarZonas() {
  let html = '<option value="">Elegí tu zona…</option>';
  menu.zonas.forEach(z => { html += '<option value="' + z.id + '">' + esc(z.nombre) + " · " + dinero.format(z.costo) + "</option>"; });
  $("#f-zona").innerHTML = html;
}

// ---------- 5. CARRITO ----------
function guardarCarrito() {
  try { localStorage.setItem("crave_carrito_v2", JSON.stringify({ simples: carrito, combos: combos })); } catch (e) { /* si no se puede guardar, no pasa nada */ }
}

function leerCarrito() {
  try {
    const guardado = JSON.parse(localStorage.getItem("crave_carrito_v2") || "{}");

    // Productos comunes: solo recuperamos los que todavía existen y no están agotados
    const simples = guardado.simples || {};
    for (const id in simples) {
      const p = productos[id];
      if (p && !p.agotado && !tieneOpciones(p) && simples[id] > 0) carrito[id] = Math.min(simples[id], MAXIMO_POR_PRODUCTO);
    }

    // Combos con gaseosas: recuperamos los que siguen siendo válidos (producto vigente y gaseosas disponibles)
    const disponibles = gaseosasDisponibles().map(g => g.id);
    (guardado.combos || []).forEach(c => {
      const p = productos[c.id];
      if (!p || p.agotado || !tieneOpciones(p)) return;
      if (!Array.isArray(c.gaseosas) || c.gaseosas.length !== p.gaseosasAElegir) return;
      if (!c.gaseosas.every(g => disponibles.includes(g))) return;
      combos.push({ uid: siguienteUid++, id: c.id, gaseosas: c.gaseosas });
    });
  } catch (e) { /* ignoramos datos guardados rotos */ }
}

function totalUnidades() {
  let total = combos.length;   // cada renglón de combos es una unidad
  for (const id in carrito) total += carrito[id];
  return total;
}

function calcularSubtotal() {
  let total = 0;
  for (const id in carrito) total += productos[id].precio * carrito[id];
  combos.forEach(c => { total += productos[c.id].precio; });
  return total;
}

function modoEntrega() { return $("#formulario").elements.modo.value; }          // "delivery" o "retiro"
function zonaElegida() { return menu.zonas.find(z => z.id === $("#f-zona").value); }
function calcularEnvio() { return (modoEntrega() === "delivery" && zonaElegida()) ? zonaElegida().costo : 0; }
function calcularTotal() { return calcularSubtotal() + calcularEnvio(); }

// Al redibujar se pierde el foco del teclado: esta función se lo devuelve al botón que corresponde.
// "tarjeta" es la tarjeta donde se tocó (null si se tocó dentro del cajón).
function devolverFoco(id, accion, tarjeta) {
  let nuevo = null;
  if (tarjeta) nuevo = $('[data-accion="' + accion + '"]', tarjeta) || $("[data-accion]", tarjeta);
  else nuevo = $('#lineas [data-id="' + id + '"][data-accion="' + accion + '"]') || $("#lineas [data-accion], #lineas [data-quitar-combo]") || $("#vacio-cerrar");
  if (nuevo) nuevo.focus();
}

// Suma o resta una unidad. "boton" es el botón que se tocó.
function cambiarCantidad(id, cambio, boton) {
  // Productos con gaseosas a elegir: "+" abre la ventana de elección y "-" quita el último que se agregó
  if (tieneOpciones(productos[id])) {
    if (cambio > 0) abrirSelector(id, null, boton);
    else quitarUltimoCombo(id, boton);
    return;
  }

  const antes = carrito[id] || 0;
  const despues = Math.max(0, Math.min(MAXIMO_POR_PRODUCTO, antes + cambio));
  if (despues === antes) return;

  const tarjeta = boton.closest(".tarjeta");
  if (cambio > 0 && tarjeta) volarAlCarrito(boton);   // animación: se hace antes de redibujar

  if (despues === 0) delete carrito[id]; else carrito[id] = despues;
  guardarCarrito();
  actualizarTodo();
  devolverFoco(id, cambio > 0 ? "sumar" : "restar", tarjeta);

  anunciar(productos[id].nombre + ": " + despues + " en el pedido. Total " + dinero.format(calcularTotal()) + ".");
}

// ----- Productos con gaseosas a elegir (Familiar, menú niños) -----

// Quita la última unidad que se agregó de ese producto (el "-" de la tarjeta)
function quitarUltimoCombo(id, boton) {
  const indice = combos.map(c => c.id).lastIndexOf(id);
  if (indice === -1) return;
  const tarjeta = boton.closest(".tarjeta");
  combos.splice(indice, 1);
  guardarCarrito();
  actualizarTodo();
  devolverFoco(id, "restar", tarjeta);
  anunciar(productos[id].nombre + ": " + cantidadDe(id) + " en el pedido. Total " + dinero.format(calcularTotal()) + ".");
}

// Quita un renglón puntual desde el cajón del pedido (botón "Quitar")
function quitarCombo(uid) {
  const combo = combos.find(c => c.uid === uid);
  if (!combo) return;
  combos = combos.filter(c => c.uid !== uid);
  guardarCarrito();
  actualizarTodo();
  anunciar(productos[combo.id].nombre + " quitado del pedido. Total " + dinero.format(calcularTotal()) + ".");
  const siguiente = $("#lineas [data-quitar-combo], #lineas [data-accion]") || $("#vacio-cerrar");
  if (siguiente) siguiente.focus();
}

// Abre la ventana para elegir las gaseosas. Si "uid" es null es un combo nuevo; si no, se edita ese renglón.
function abrirSelector(id, uid, origen) {
  const p = productos[id];
  const cantidades = {};   // gaseosa -> cuántas. Ejemplo: { "beb-coca": 2, "beb-sprite": 1 }

  if (uid === null) {
    if (cantidadDe(id) >= MAXIMO_POR_PRODUCTO) {
      anunciar("El máximo es de " + MAXIMO_POR_PRODUCTO + " unidades de " + p.nombre + ".");
      return;
    }
  } else {
    // Al editar arrancamos con lo que ya estaba elegido (salvo las gaseosas que se hayan agotado)
    const combo = combos.find(c => c.uid === uid);
    const disponibles = gaseosasDisponibles().map(g => g.id);
    if (combo) combo.gaseosas.forEach(g => { if (disponibles.includes(g)) cantidades[g] = (cantidades[g] || 0) + 1; });
  }

  selector = { id: id, uid: uid, origen: origen, cantidades: cantidades };
  dibujarSelector();
  $("#selector").showModal();
  const primero = $('#selector-caja [data-delta="1"]:not([disabled])');
  if (primero) primero.focus();
}

function totalElegido() {
  return Object.values(selector.cantidades).reduce((suma, n) => suma + n, 0);
}

// Dibuja el contenido de la ventana: una fila por gaseosa con "-", cantidad y "+"
function dibujarSelector() {
  const p = productos[selector.id];
  const necesarias = p.gaseosasAElegir;
  const elegidas = totalElegido();
  const completo = elegidas === necesarias;
  const sabores = gaseosasDisponibles();

  const filas = sabores.map(s => {
    const n = selector.cantidades[s.id] || 0;
    // Con una sola gaseosa a elegir el "+" siempre sirve (cambia la elección). Con varias, se bloquea al completar.
    const bloquearMas = completo && necesarias > 1;
    return `
      <li>
        <span class="sabor-nombre">${esc(s.nombre)}</span>
        <div class="cantidad" role="group" aria-label="Cantidad de ${esc(s.nombre)}">
          <button type="button" data-sabor="${esc(s.id)}" data-delta="-1" aria-label="Quitar ${esc(s.nombre)}" ${n === 0 ? "disabled" : ""}>${iconoChico("menos")}</button>
          <output>${n}</output>
          <button type="button" data-sabor="${esc(s.id)}" data-delta="1" aria-label="Agregar ${esc(s.nombre)}" ${bloquearMas ? "disabled" : ""}>${iconoChico("mas")}</button>
        </div>
      </li>`;
  }).join("");

  const consigna = necesarias === 1 ? "Elegí 1 gaseosa." : "Elegí " + necesarias + " gaseosas. Se pueden repetir.";
  $("#selector-caja").innerHTML = `
    <h3 id="selector-titulo">Elegí tus gaseosas</h3>
    <p class="ayuda">${esc(p.nombre)}. ${consigna}</p>
    ${sabores.length > 0 ? '<ul class="sabores">' + filas + "</ul>" : '<p class="error">Hoy no hay gaseosas disponibles para este producto.</p>'}
    <p class="contador-sabores">Elegiste ${elegidas} de ${necesarias}</p>
    <div class="selector-botones">
      <button type="button" class="btn btn-borde" data-selector="cancelar">Cancelar</button>
      <button type="button" class="btn btn-rojo" data-selector="confirmar" ${completo ? "" : "disabled"}>${selector.uid === null ? "Agregar al pedido" : "Guardar cambios"}</button>
    </div>`;
  $("#selector-aviso").textContent = "Elegiste " + elegidas + " de " + necesarias + ".";
}

// Suma o resta una gaseosa en la ventana
function cambiarSabor(idSabor, cambio) {
  const necesarias = productos[selector.id].gaseosasAElegir;

  if (necesarias === 1) {
    // Con una sola gaseosa, tocar otra "+" reemplaza la anterior
    if (cambio > 0) selector.cantidades = { [idSabor]: 1 };
    else delete selector.cantidades[idSabor];
  } else {
    const nuevo = (selector.cantidades[idSabor] || 0) + cambio;
    if (nuevo < 0) return;
    if (cambio > 0 && totalElegido() >= necesarias) return;
    if (nuevo === 0) delete selector.cantidades[idSabor]; else selector.cantidades[idSabor] = nuevo;
  }
  dibujarSelector();

  // Al redibujar se pierde el foco: volvemos al mismo botón (o al otro de la misma gaseosa si quedó bloqueado)
  const mismo = $('#selector-caja [data-sabor="' + idSabor + '"][data-delta="' + cambio + '"]');
  const otro = $('#selector-caja [data-sabor="' + idSabor + '"]:not([disabled])');
  const destino = (mismo && !mismo.disabled) ? mismo : otro;
  if (destino) destino.focus();
}

// Guarda lo elegido en el carrito (como renglón nuevo o cambiando el renglón que se estaba editando)
function confirmarSelector() {
  const p = productos[selector.id];
  if (totalElegido() !== p.gaseosasAElegir) return;

  // De { "beb-coca": 2, "beb-sprite": 1 } pasamos a la lista [ "beb-coca", "beb-coca", "beb-sprite" ]
  const lista = [];
  for (const idGaseosa in selector.cantidades) {
    for (let i = 0; i < selector.cantidades[idGaseosa]; i++) lista.push(idGaseosa);
  }

  const { id, uid, origen } = selector;   // los copiamos antes de cerrar, porque al cerrar "selector" se vacía
  const tarjeta = origen ? origen.closest(".tarjeta") : null;
  $("#selector").close();

  if (uid === null) {
    if (tarjeta) volarAlCarrito(origen);   // animación: se hace antes de redibujar
    combos.push({ uid: siguienteUid++, id: id, gaseosas: lista });
  } else {
    const combo = combos.find(c => c.uid === uid);
    if (combo) combo.gaseosas = lista;
  }
  guardarCarrito();
  actualizarTodo();
  anunciar(productos[id].nombre + " con " + textoGaseosas(lista) + ". Total " + dinero.format(calcularTotal()) + ".");

  if (tarjeta) devolverFoco(id, "sumar", tarjeta);
  else {
    const editar = $('[data-editar-combo="' + uid + '"]');
    if (editar) editar.focus();
  }
}

// Vuelve a dibujar todo lo que depende del carrito
function actualizarTodo() {
  const unidades = totalUnidades();
  const subtotal = calcularSubtotal();
  const envio = calcularEnvio();
  const total = subtotal + envio;
  const entrega = modoEntrega() === "delivery";

  // Contador de arriba y píldora flotante del móvil
  $("#insignia").textContent = unidades;
  $("#insignia").hidden = unidades === 0;
  $("#pildora").classList.toggle("visible", unidades > 0);
  $("#pildora-texto").textContent = "Ver pedido (" + unidades + ")";
  $("#pildora-total").textContent = dinero.format(subtotal);

  // Botones "+" y selectores de cantidad de todas las tarjetas
  $$(".control").forEach(c => { c.innerHTML = controlHTML(c.dataset.id); });

  // Cajón: lista de productos y resumen de importes
  const vistaFinalActiva = !$("#vista-final").hidden;
  $("#vacio").hidden = unidades > 0 || vistaFinalActiva;
  $("#con-pedido").hidden = unidades === 0;
  $("#cajon-pie").hidden = unidades === 0 || vistaFinalActiva;

  let lineas = "";
  for (const id in carrito) {
    const p = productos[id];
    lineas += `
      <li>
        <div><b>${esc(p.nombre)}</b><div class="sub">${dinero.format(p.precio)} c/u</div></div>
        <div class="importe">${dinero.format(p.precio * carrito[id])}</div>
        <div class="cantidad" role="group" aria-label="Cantidad de ${esc(p.nombre)}">
          <button type="button" data-accion="restar" data-id="${id}" aria-label="Quitar una unidad de ${esc(p.nombre)}">${iconoChico("menos")}</button>
          <output>${carrito[id]}</output>
          <button type="button" data-accion="sumar" data-id="${id}" aria-label="Agregar una unidad de ${esc(p.nombre)}">${iconoChico("mas")}</button>
        </div>
      </li>`;
  }
  // Productos con gaseosas: un renglón por unidad, con el detalle y los botones para cambiarlo o quitarlo
  combos.forEach(c => {
    const p = productos[c.id];
    lineas += `
      <li>
        <div><b>${esc(p.nombre)}</b><div class="sub">${esc(textoGaseosas(c.gaseosas))}</div></div>
        <div class="importe">${dinero.format(p.precio)}</div>
        <div class="acciones-linea">
          <button type="button" class="copiar" data-editar-combo="${c.uid}">Cambiar gaseosas</button>
          <button type="button" class="copiar" data-quitar-combo="${c.uid}" aria-label="Quitar ${esc(p.nombre)} del pedido">Quitar</button>
        </div>
      </li>`;
  });
  $("#lineas").innerHTML = lineas;

  $("#resumen").innerHTML = `
    <div><span>Subtotal</span><span>${dinero.format(subtotal)}</span></div>
    <div><span>Envío</span><span>${entrega ? (zonaElegida() ? dinero.format(envio) : "Elegí tu zona") : "Retiro sin costo"}</span></div>
    <div class="total"><span>Total</span><span>${dinero.format(total)}</span></div>`;
  $("#total-pie").textContent = dinero.format(total);
  $("#pago-importe").textContent = dinero.format(total);
}

// ---------- 6. FORMULARIO ----------
// Muestra u oculta los grupos según lo que eligió la persona
function sincronizarFormulario() {
  const f = $("#formulario").elements;
  $("#grupo-delivery").hidden = f.modo.value !== "delivery";
  $("#legend-horario").textContent = f.modo.value === "delivery" ? "Horario de entrega" : "Horario de retiro";
  $("#pago-transferencia").hidden = f.pago.value !== "transferencia";
  $("#pago-efectivo").hidden = f.pago.value !== "efectivo";
  actualizarTodo();
}

function mostrarError(campo, mensaje) {
  const texto = $("#e-" + campo.id.slice(2));
  campo.setAttribute("aria-invalid", "true");
  texto.textContent = mensaje;
  texto.hidden = false;
}

function limpiarError(campo) {
  const texto = $("#e-" + campo.id.slice(2));
  campo.removeAttribute("aria-invalid");
  if (texto) { texto.hidden = true; texto.textContent = ""; }
}

// Revisa los datos antes de enviar. Devuelve la lista de campos con problemas.
function validarFormulario() {
  const f = $("#formulario").elements;
  const conError = [];

  function exigir(selector, estaBien, mensaje) {
    const campo = $(selector);
    limpiarError(campo);
    if (!estaBien) { mostrarError(campo, mensaje); conError.push(campo); }
  }

  exigir("#f-nombre", f.nombre.value.trim().length >= 3, "Escribí tu nombre y apellido.");
  exigir("#f-telefono", f.telefono.value.replace(/\D/g, "").length >= 8, "Escribí un teléfono con característica, por ejemplo 3493 15 000000.");

  if (f.modo.value === "delivery") {
    exigir("#f-calle", f.calle.value.trim().length >= 4, "Escribí la calle y el número.");
    exigir("#f-zona", !!zonaElegida(), "Elegí la zona para calcular el envío.");
  }
  exigir("#f-hora", horaElegida() !== "", "Elegí en qué horario lo querés.");
  if (f.pago.value === "efectivo" && f.efectivo.value.trim() !== "") {
    const abona = parseInt(f.efectivo.value.replace(/\D/g, ""), 10) || 0;
    exigir("#f-efectivo", abona >= calcularTotal(), "El importe tiene que ser igual o mayor al total (" + dinero.format(calcularTotal()) + ").");
  }
  return conError;
}

// Arma el pedido tal como lo espera el servidor C# (clase PedidoNuevo)
function leerPedidoDelFormulario() {
  const f = $("#formulario").elements;
  const items = [];
  for (const id in carrito) items.push({ productoId: id, cantidad: carrito[id], gaseosas: [] });
  combos.forEach(c => items.push({ productoId: c.id, cantidad: 1, gaseosas: c.gaseosas }));
  return {
    nombre: f.nombre.value.trim(),
    telefono: f.telefono.value.trim(),
    modo: f.modo.value,
    calle: f.calle.value.trim(),
    piso: f.piso.value.trim(),
    zonaId: f.zona.value,
    referencias: f.referencias.value.trim(),
    hora: horaElegida(),
    pago: f.pago.value,
    abonaCon: parseInt(f.efectivo.value.replace(/\D/g, ""), 10) || 0,
    notas: f.notas.value.trim(),
    items: items,
    website: f.website.value,   // campo oculto: tiene que ir vacío
    segundos: Math.round((Date.now() - paginaAbiertaEn) / 1000)
  };
}

// Manda el pedido. Devuelve { ok: true, pedido } o { ok: false, errores: [...] }
async function enviarPedido(datos) {
  if (MODO_DEMO) {
    await new Promise(r => setTimeout(r, 700));   // simula la espera del servidor
    ocupadosDemo[datos.hora] = (ocupadosDemo[datos.hora] || 0) + 1;   // el horario pierde un lugar
    return { ok: true, pedido: pedidoDeEjemplo(datos) };
  }
  try {
    const respuesta = await fetch("/api/pedidos", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(datos)
    });
    const json = await respuesta.json().catch(() => ({}));   // algunos rechazos (ej.: 413) no traen texto
    // 400 = datos inválidos, 409 = el horario se llenó justo antes, 429 = demasiados envíos seguidos. El servidor manda "errores".
    if (!respuesta.ok) return { ok: false, errores: json.errores || ["No pudimos registrar el pedido. Probá de nuevo."], conflicto: respuesta.status === 409 };
    return { ok: true, pedido: json };
  } catch (e) {
    return { ok: false, errores: ["No hay conexión con el servidor. Revisá tu internet y probá de nuevo."] };
  }
}

// Solo para el modo demo: calcula lo mismo que calcularía el servidor
function pedidoDeEjemplo(datos) {
  const lineas = datos.items.map(i => ({
    nombre: productos[i.productoId].nombre, cantidad: i.cantidad, precio: productos[i.productoId].precio,
    detalle: (i.gaseosas && i.gaseosas.length > 0) ? textoGaseosas(i.gaseosas) : null
  }));
  const subtotal = lineas.reduce((suma, l) => suma + l.precio * l.cantidad, 0);
  const zona = menu.zonas.find(z => z.id === datos.zonaId);
  const envio = (datos.modo === "delivery" && zona) ? zona.costo : 0;
  const total = subtotal + envio;
  return {
    numero: "CR-" + (1000 + Math.floor(Math.random() * 9000)),
    nombre: datos.nombre,
    hora: datos.hora,
    direccion: datos.modo === "delivery" ? datos.calle + (datos.piso ? " (" + datos.piso + ")" : "") + ", " + (zona ? zona.nombre : "") : "Retiro en el local",
    subtotal: subtotal, envio: envio, total: total, lineas: lineas,
    vuelto: (datos.pago === "efectivo" && datos.abonaCon > 0) ? datos.abonaCon - total : 0
  };
}

async function alConfirmar(evento) {
  evento.preventDefault();
  $("#errores-servidor").hidden = true;

  const conError = validarFormulario();
  if (conError.length > 0) {
    conError[0].focus();
    anunciar("Faltan " + conError.length + " datos para confirmar el pedido.");
    return;
  }

  const boton = $("#boton-confirmar");
  boton.disabled = true;
  boton.textContent = "Enviando…";

  const datos = leerPedidoDelFormulario();
  const resultado = await enviarPedido(datos);

  boton.textContent = "Confirmar pedido";

  if (!resultado.ok) {
    await refrescarFranjas();   // los lugares pudieron cambiar: mostramos los números de ahora
    const caja = $("#errores-servidor");
    caja.innerHTML = "<ul>" + resultado.errores.map(e => "<li>" + esc(e) + "</li>").join("") + "</ul>";
    caja.hidden = false;
    caja.scrollIntoView({ block: "nearest" });
    return;
  }
  actualizarBotonConfirmar();

  carrito = {};
  combos = [];
  guardarCarrito();
  $("#formulario").reset();
  mostrarConfirmacion(resultado.pedido, datos);
  sincronizarFormulario();
  refrescarFranjas();       // para el próximo pedido
}

// ---------- 7. CAJÓN ----------
function abrirCajon(desde) {
  quienAbrio = desde || document.activeElement;
  actualizarTodo();
  refrescarFranjas();   // los lugares cambian todo el tiempo: pedimos los números de ahora
  const cajon = $("#cajon"), velo = $("#velo");
  cajon.hidden = false;
  velo.hidden = false;
  document.documentElement.classList.add("bloqueado");
  requestAnimationFrame(() => {          // esperamos un cuadro para que la transición se vea
    cajon.classList.add("abierto");
    velo.classList.add("abierto");
    $("#cerrar").focus();
  });
}

function cerrarCajon() {
  const cajon = $("#cajon"), velo = $("#velo");
  cajon.classList.remove("abierto");
  velo.classList.remove("abierto");
  document.documentElement.classList.remove("bloqueado");
  setTimeout(() => { cajon.hidden = true; velo.hidden = true; }, 350);
  if (quienAbrio && quienAbrio.focus) quienAbrio.focus();
  if (!$("#vista-final").hidden) volverAlPedido();
}

// Mantiene el foco del teclado dentro del cajón mientras está abierto
function atraparFoco(evento) {
  if ($("#selector").open) return;   // la ventana de gaseosas maneja sola el foco y la tecla Escape
  if ($("#cajon").hidden) return;
  if (evento.key === "Escape") { cerrarCajon(); return; }
  if (evento.key !== "Tab") return;
  const foco = $$("#cajon button:not([disabled]), #cajon input, #cajon select, #cajon textarea, #cajon a[href]").filter(el => el.offsetParent !== null);
  if (foco.length === 0) return;
  const primero = foco[0], ultimo = foco[foco.length - 1];
  if (evento.shiftKey && document.activeElement === primero) { evento.preventDefault(); ultimo.focus(); }
  else if (!evento.shiftKey && document.activeElement === ultimo) { evento.preventDefault(); primero.focus(); }
}

// ---------- 8. CONFIRMACIÓN ----------
function copiarTexto(texto, boton, etiqueta) {
  const listo = () => { boton.textContent = "Copiado"; setTimeout(() => { boton.textContent = etiqueta; }, 1600); };
  const planB = () => {
    const area = document.createElement("textarea");
    area.value = texto; area.style.position = "fixed"; area.style.opacity = "0";
    document.body.appendChild(area); area.select();
    try { if (document.execCommand("copy")) listo(); } catch (e) { /* nada */ }
    area.remove();
  };
  if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(texto).then(listo, planB);
  else planB();
}

// Enlace para que el cliente siga su pedido. El servidor manda "codigo" (largo y al azar); en el modo demo no hay y devuelve "".
function enlaceSeguimiento(pedido) {
  return pedido.codigo ? location.origin + "/seguimiento.html?c=" + encodeURIComponent(pedido.codigo) : "";
}

function textoParaWhatsApp(pedido, datos) {
  const l = ["Pedido CRAVE " + pedido.numero, "Nombre: " + pedido.nombre, "Teléfono: " + datos.telefono];
  l.push("Entrega: " + (datos.modo === "delivery" ? "delivery a " + pedido.direccion + (datos.referencias ? ". Ref.: " + datos.referencias : "") : "retiro en el local"));
  l.push("Horario: " + datos.hora + " hs", "");
  pedido.lineas.forEach(x => l.push(x.cantidad + " x " + x.nombre + (x.detalle ? " (" + x.detalle + ")" : "") + " — " + dinero.format(x.cantidad * x.precio)));
  l.push("", "Subtotal: " + dinero.format(pedido.subtotal), "Envío: " + dinero.format(pedido.envio), "Total: " + dinero.format(pedido.total));
  l.push("Pago: " + (datos.pago === "transferencia" ? "transferencia (envío comprobante)" : "efectivo" + (datos.abonaCon ? ", abono con " + dinero.format(datos.abonaCon) : ", pago exacto")));
  if (datos.notas) l.push("Aclaraciones: " + datos.notas);
  if (pedido.codigo) l.push("", "Seguí tu pedido: " + enlaceSeguimiento(pedido));
  return l.join("\n");
}

function mostrarConfirmacion(pedido, datos) {
  const mensaje = textoParaWhatsApp(pedido, datos);
  const pago = datos.pago === "transferencia"
    ? `<div class="caja-pago">
         <dl><dt>Alias</dt><dd translate="no">${esc(CONFIG.alias)}</dd><dt>Titular</dt><dd>${esc(CONFIG.titular)}</dd><dt>Importe</dt><dd>${dinero.format(pedido.total)}</dd></dl>
         <button class="copiar" type="button" id="final-copiar-alias">Copiar alias</button>
         <p class="ayuda">Transferí el importe y mandanos el comprobante por WhatsApp. Empezamos a preparar el pedido cuando se acredita.</p>
       </div>`
    : `<div class="aviso-final">Pagás en efectivo ${datos.modo === "delivery" ? "cuando te lo llevamos" : "al retirar"}. ${pedido.vuelto > 0 ? "Vas a abonar con " + dinero.format(datos.abonaCon) + "; el vuelto es " + dinero.format(pedido.vuelto) + "." : "Tené " + dinero.format(pedido.total) + " justo."}</div>`;

  $("#vista-final").innerHTML = `
    <svg class="tilde" viewBox="0 0 88 88" aria-hidden="true"><circle cx="44" cy="44" r="36"/><path d="M28 45l11 11 22-24"/></svg>
    <h3 id="final-titulo" tabindex="-1">Pedido <span style="white-space:nowrap">${esc(pedido.numero)}</span> registrado</h3>
    <p class="ayuda" style="margin-top:8px">${datos.modo === "delivery" ? "Te lo llevamos a " + esc(pedido.direccion) : "Lo retirás en el local"}. Horario: ${esc(datos.hora)} hs.</p>
    <ul class="lineas-pedido" style="margin-top:14px">
      ${pedido.lineas.map(x => `<li><div><b>${x.cantidad} × ${esc(x.nombre)}</b>${x.detalle ? '<div class="sub">' + esc(x.detalle) + "</div>" : ""}</div><div class="importe">${dinero.format(x.cantidad * x.precio)}</div></li>`).join("")}
    </ul>
    <div class="resumen">
      <div><span>Subtotal</span><span>${dinero.format(pedido.subtotal)}</span></div>
      <div><span>Envío</span><span>${dinero.format(pedido.envio)}</span></div>
      <div class="total"><span>Total</span><span>${dinero.format(pedido.total)}</span></div>
    </div>
    <div class="pila">
      ${pago}
      <a class="btn btn-rojo" href="https://wa.me/${CONFIG.whatsapp}?text=${encodeURIComponent(mensaje)}" target="_blank" rel="noopener">Enviar pedido por WhatsApp</a>
      ${pedido.codigo ? `<a class="btn btn-borde" id="final-seguir" href="${esc(enlaceSeguimiento(pedido))}" target="_blank" rel="noopener">Seguir mi pedido</a>` : ""}
      <button class="btn btn-borde" type="button" id="final-copiar-resumen">Copiar resumen del pedido</button>
      <button class="btn btn-borde" type="button" id="final-otro">Hacer otro pedido</button>
      ${MODO_DEMO ? '<p class="aviso-final">Demo: acá no se envía nada. En el proyecto con servidor, el pedido se valida y se guarda en C#.</p>' : ""}
    </div>`;

  $("#vista-pedido").hidden = true;
  $("#vista-final").hidden = false;
  $("#titulo-cajon").textContent = "Pedido confirmado";

  const copiarAlias = $("#final-copiar-alias");
  if (copiarAlias) copiarAlias.addEventListener("click", e => copiarTexto(CONFIG.alias, e.currentTarget, "Copiar alias"));
  $("#final-copiar-resumen").addEventListener("click", e => copiarTexto(mensaje, e.currentTarget, "Copiar resumen del pedido"));
  $("#final-otro").addEventListener("click", () => { cerrarCajon(); });

  $("#cajon-cuerpo").scrollTop = 0;
  $("#final-titulo").focus();
  lanzarConfeti();
}

function volverAlPedido() {
  $("#vista-final").hidden = true;
  $("#vista-final").innerHTML = "";
  $("#confeti").innerHTML = "";
  $("#vista-pedido").hidden = false;
  $("#titulo-cajon").textContent = "Tu pedido";
  actualizarTodo();
}

// ---------- 9. ANIMACIONES ----------
// Un punto rojo vuela desde el botón "+" hasta el botón "Mi pedido"
function volarAlCarrito(origen) {
  if (sinMovimiento.matches) { rebotarInsignia(); return; }
  const a = origen.getBoundingClientRect();
  const b = $("#abrir-pedido").getBoundingClientRect();
  const dx = (b.left + b.width / 2) - (a.left + a.width / 2);
  const dy = (b.top + b.height / 2) - (a.top + a.height / 2);

  const punto = document.createElement("div");
  punto.className = "punto-volador";
  punto.style.left = (a.left + a.width / 2 - 9) + "px";
  punto.style.top = (a.top + a.height / 2 - 9) + "px";
  document.body.appendChild(punto);

  const vuelo = punto.animate(
    [{ transform: "translate(0, 0) scale(1)" }, { transform: "translate(" + dx + "px, " + dy + "px) scale(.4)" }],
    { duration: 650, easing: "cubic-bezier(.5, 0, .8, .4)" }
  );
  vuelo.onfinish = () => { punto.remove(); rebotarInsignia(); };
}

function rebotarInsignia() {
  if (sinMovimiento.matches) return;
  $("#insignia").animate(
    [{ transform: "scale(1)" }, { transform: "scale(1.6)" }, { transform: "scale(1)" }],
    { duration: 350, easing: "ease-out" }
  );
}

// Chevrones rojos y blancos que caen al confirmar el pedido
function lanzarConfeti() {
  if (sinMovimiento.matches) return;
  const colores = ["#d62828", "#f5f5f5", "#d62828"];
  let html = "";
  for (let i = 0; i < 24; i++) {
    const x = Math.round(Math.random() * 100);
    const giro = Math.round(Math.random() * 360);
    const retraso = (Math.random() * 0.6).toFixed(2);
    html += '<i style="--x:' + x + "%;--c:" + colores[i % 3] + ";--r:" + giro + "deg;--d:" + retraso + 's"></i>';
  }
  $("#confeti").innerHTML = html;
}

// Las tarjetas suben un poquito cuando entran en pantalla
function activarReveal() {
  const elementos = $$(".reveal");
  if (!("IntersectionObserver" in window)) { elementos.forEach(el => el.classList.add("visto")); return; }
  const vigia = new IntersectionObserver(entradas => {
    entradas.forEach(entrada => {
      if (entrada.isIntersecting) {
        entrada.target.classList.add("visto");
        vigia.unobserve(entrada.target);
      }
    });
  }, { threshold: 0.12 });
  elementos.forEach((el, i) => {
    el.style.setProperty("--retraso", (i % 4) * 70 + "ms");
    vigia.observe(el);
  });
}

// Marca en la barra de arriba la sección que se está mirando:
// es la última sección cuyo borde de arriba ya pasó la barra.
let categoriaActiva = "";
function marcarPastilla() {
  let actual = "combos";
  $$("section[data-cat]").forEach(seccion => {
    if (seccion.getBoundingClientRect().top <= 190) actual = seccion.dataset.cat;
  });
  if (actual === categoriaActiva) return;
  categoriaActiva = actual;
  $$(".pastilla").forEach(p => {
    const activa = p.dataset.cat === actual;
    p.setAttribute("aria-current", String(activa));
    if (activa) $("#pastillas").scrollTo({ left: p.offsetLeft - 16, behavior: "smooth" });   // la pastilla activa queda a la vista
  });
}

let esperandoCuadro = false;
function seguirSeccion() {
  window.addEventListener("scroll", () => {
    if (esperandoCuadro) return;
    esperandoCuadro = true;
    requestAnimationFrame(() => { marcarPastilla(); esperandoCuadro = false; });
  }, { passive: true });
  marcarPastilla();
}

// ---------- 10. ARRANQUE ----------
// Si la foto de un producto no carga (el archivo todavía no está copiado o la ruta tiene un error de tipeo),
// la tarjeta muestra el dibujo del producto en vez de una imagen rota.
// "error" no sube por la página como los clicks, así que se escucha en la fase de captura (el "true" del final).
document.addEventListener("error", evento => {
  const foto = evento.target;
  if (!(foto instanceof HTMLImageElement) || !foto.classList.contains("foto")) return;
  foto.outerHTML = icono(foto.dataset.icono || "pollo");
}, true);

// Un solo "escuchador" de clicks para todos los botones "+", "-" y los enlaces internos
document.addEventListener("click", evento => {
  if (evento.target.closest("[data-reintentar]")) { refrescarFranjas(); return; }

  // Ventana de gaseosas y renglones de combos
  const sabor = evento.target.closest("[data-sabor]");
  if (sabor) { cambiarSabor(sabor.dataset.sabor, Number(sabor.dataset.delta)); return; }
  const accionSelector = evento.target.closest("[data-selector]");
  if (accionSelector) {
    if (accionSelector.dataset.selector === "confirmar") confirmarSelector();
    else $("#selector").close();
    return;
  }
  const editar = evento.target.closest("[data-editar-combo]");
  if (editar) {
    const uid = Number(editar.dataset.editarCombo);
    const combo = combos.find(c => c.uid === uid);
    if (combo) abrirSelector(combo.id, uid, editar);
    return;
  }
  const quitar = evento.target.closest("[data-quitar-combo]");
  if (quitar) { quitarCombo(Number(quitar.dataset.quitarCombo)); return; }

  const boton = evento.target.closest("[data-accion]");
  if (boton) {
    cambiarCantidad(boton.dataset.id, boton.dataset.accion === "sumar" ? 1 : -1, boton);
    return;
  }
  const enlace = evento.target.closest('a[href^="#"]');
  if (enlace) {
    const destino = document.getElementById(enlace.getAttribute("href").slice(1));
    if (destino) { evento.preventDefault(); destino.scrollIntoView(); }
  }
});

// Botón de pausa: frena la cinta, las filas y el anillo (clase "pausado" en <html>)
$("#pausa").addEventListener("click", e => {
  const pausado = document.documentElement.classList.toggle("pausado");
  e.currentTarget.setAttribute("aria-pressed", String(pausado));
  e.currentTarget.setAttribute("aria-label", pausado ? "Reanudar animaciones" : "Pausar animaciones");
  $("use", e.currentTarget).setAttribute("href", pausado ? "#i-play" : "#i-pausa");
});

$("#abrir-pedido").addEventListener("click", e => abrirCajon(e.currentTarget));
$("#pildora").addEventListener("click", e => abrirCajon(e.currentTarget));
$("#banda-pedido").addEventListener("click", e => abrirCajon(e.currentTarget));
$("#cerrar").addEventListener("click", cerrarCajon);
$("#velo").addEventListener("click", cerrarCajon);
$("#vacio-cerrar").addEventListener("click", () => { cerrarCajon(); $("#carta").scrollIntoView(); });
document.addEventListener("keydown", atraparFoco);

// Ventana de gaseosas: se cierra tocando el fondo oscuro, y al cerrarse (también con Escape) se limpia lo elegido
$("#selector").addEventListener("click", e => { if (e.target === e.currentTarget) e.currentTarget.close(); });
$("#selector").addEventListener("close", () => { selector = null; });

$("#formulario").addEventListener("change", sincronizarFormulario);
$("#formulario").addEventListener("input", e => {
  if (e.target.getAttribute("aria-invalid")) limpiarError(e.target);
  if (e.target.name === "hora") limpiarError($("#f-hora"));   // los horarios son varios radios dentro de un grupo
});
$("#formulario").addEventListener("submit", alConfirmar);
$("#copiar-alias").addEventListener("click", e => copiarTexto(CONFIG.alias, e.currentTarget, "Copiar alias"));
// Enlace de WhatsApp de la banda final: abre un chat con el número de CONFIG (el mismo que recibe los pedidos)
$("#enlace-whatsapp").href = "https://wa.me/" + CONFIG.whatsapp + "?text=" + encodeURIComponent("Hola CRAVE, quiero hacer una consulta.");
$("#pago-alias").textContent = CONFIG.alias;
$("#pago-titular").textContent = CONFIG.titular;

async function iniciar() {
  dibujarFilas();
  dibujarCinta();
  try {
    menu = await cargarMenu();
  } catch (e) {
    $("#menu").innerHTML = '<p class="nota" style="padding-block:40px">No pudimos cargar la carta. Recargá la página para intentar de nuevo.</p>';
    return;
  }
  menu.categorias.forEach(c => c.productos.forEach(p => { productos[p.id] = p; }));
  leerCarrito();

  dibujarPastillas();
  dibujarAntojos();
  dibujarCombos();
  dibujarCarta();
  llenarZonas();
  await cargarFranjas();
  dibujarFranjas();
  sincronizarFormulario();

  activarReveal();
  seguirSeccion();
}
iniciar();
