// Panel de administración de CRAVE.
// Habla con la API /api/admin/... (el servidor exige la cookie de sesión en todas, salvo en el login).

const ZONA_HORARIA = "America/Argentina/Buenos_Aires";
const ACTUALIZAR_CADA_MS = 15000;   // los pedidos se vuelven a pedir cada 15 segundos

// Qué botón principal le toca a cada estado: [estado siguiente, texto del botón]
const SIGUIENTE = {
  nuevo: ["preparando", "Empezar a preparar"],
  preparando: ["listo", "Marcar listo"],
  listo: ["entregado", "Marcar entregado"]
};
// Para deshacer un toque equivocado
const ANTERIOR = { preparando: "nuevo", listo: "preparando", entregado: "listo" };
const ESTADOS_ACTIVOS = ["nuevo", "preparando", "listo"];

// true si es una transferencia que todavía no se marcó como recibida. El servidor aclara si está "pendiente"
// (recién llegó el pedido) o "demorado" (pasaron más minutos que los de la pestaña Horarios).
// Un pedido cancelado nunca cuenta: el servidor lo marca "sin_pago".
function sinPagoConfirmado(p) {
  return p.estadoPago === "pendiente" || p.estadoPago === "demorado";
}

const $ = (id) => document.getElementById(id);

// ---------- Estado de la pantalla ----------
let pedidos = [];
let productos = [];
let config = null;            // configuración del local (abierto/cerrado, horarios, cupo...)
let vista = "pedidos";        // "pedidos", "productos" o "config"
let filtro = "activos";       // "activos", "sinpago", "entregado", "cancelado" o "todos"
let confirmando = null;       // "accion:id" del botón que espera un segundo toque (cancelar, o preparar sin pago confirmado)
let ocupado = false;          // true mientras se manda un cambio al servidor (evita dobles toques)
let temporizador = null;
let idsNuevosVistos = null;   // ids de pedidos "nuevo" ya vistos (null = todavía no cargamos nada)
let sonidoActivo = leerGuardado("crave_sonido") === "1";
let audio = null;

// localStorage puede fallar (ventana privada, etc.), por eso va dentro de try/catch
function leerGuardado(clave) {
  try { return localStorage.getItem(clave); } catch { return null; }
}
function guardar(clave, valor) {
  try { localStorage.setItem(clave, valor); } catch { /* si no se puede, no pasa nada */ }
}

// ---------- Utilidades ----------

// IMPORTANTE: todo texto que viene de un cliente (nombre, dirección, notas...) pasa por esc() antes de
// meterse en el HTML. Así nadie puede escribir código en su pedido y que se ejecute en tu panel.
function esc(texto) {
  return String(texto ?? "")
    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

function plata(numero) {
  return "$ " + Number(numero).toLocaleString("es-AR");
}

function horaLocal(fechaIso) {
  return new Date(fechaIso).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit", timeZone: ZONA_HORARIA });
}

function mostrarAviso(texto) {
  const aviso = $("aviso");
  aviso.textContent = texto;
  aviso.hidden = texto === "";
}

// Llama a la API del panel. Si el servidor dice 401 la sesión venció y volvemos al login.
async function api(ruta, metodo = "GET", cuerpo = null) {
  const opciones = { method: metodo, headers: {} };
  if (cuerpo) {
    opciones.headers["Content-Type"] = "application/json";
    opciones.body = JSON.stringify(cuerpo);
  }
  const respuesta = await fetch("/api/admin" + ruta, opciones);   // si no hay internet, fetch lanza un error
  if (respuesta.status === 401) {
    mostrarLogin("Tu sesión venció. Entrá de nuevo.");
    throw new Error("sin-sesion");
  }
  const datos = await respuesta.json().catch(() => null);
  return { ok: respuesta.ok, datos };
}

// ---------- Pantallas: login y panel ----------

function mostrarLogin(mensaje = "") {
  clearInterval(temporizador);
  temporizador = null;
  $("panel").hidden = true;
  $("vista-login").hidden = false;
  $("error").textContent = mensaje;
  $("password").focus();
}

function mostrarPanel() {
  $("vista-login").hidden = true;
  $("panel").hidden = false;
  dibujarSonido();
  refrescar();
  clearInterval(temporizador);
  temporizador = setInterval(refrescar, ACTUALIZAR_CADA_MS);
}

// Al abrir la página: ¿la cookie de sesión sigue valiendo?
async function revisarSesion() {
  try {
    const respuesta = await fetch("/api/admin/yo");
    if (respuesta.ok) mostrarPanel(); else mostrarLogin();
  } catch {
    mostrarLogin("No hay conexión con el servidor.");
  }
}

$("form-login").addEventListener("submit", async (evento) => {
  evento.preventDefault();   // evita que el navegador recargue la página
  $("error").textContent = "";
  $("boton-entrar").disabled = true;
  try {
    const respuesta = await fetch("/api/admin/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ password: $("password").value })
    });
    if (respuesta.ok) {
      $("password").value = "";
      mostrarPanel();
    } else {
      const datos = await respuesta.json().catch(() => ({}));
      $("error").textContent = datos.error || "No se pudo entrar. Probá de nuevo.";
    }
  } catch {
    $("error").textContent = "No hay conexión con el servidor.";
  } finally {
    $("boton-entrar").disabled = false;
  }
});

// ---------- Cargar datos ----------

// Se llama sola cada 15 segundos y al tocar "Actualizar".
async function refrescar() {
  if (document.hidden) return;   // con la pestaña en segundo plano no gastamos datos
  try {
    if (vista === "pedidos") await cargarPedidos();
    else if (vista === "productos") await cargarProductos();
    await cargarConfig(vista === "config" && !formularioConfigLleno);   // el estado del local se ve en todas las pestañas
    $("estado-conexion").textContent = "Actualizado " + new Date().toLocaleTimeString("es-AR", { timeZone: ZONA_HORARIA });
    $("estado-conexion").classList.remove("mal");
  } catch (error) {
    if (error.message === "sin-sesion") return;
    $("estado-conexion").textContent = "Sin conexión. Reintentando...";
    $("estado-conexion").classList.add("mal");
  }
}

async function cargarPedidos() {
  const { ok, datos } = await api("/pedidos");
  if (!ok) throw new Error("fallo");
  pedidos = datos;
  avisarNuevos();
  dibujarPedidos();
}

async function cargarProductos() {
  const { ok, datos } = await api("/productos");
  if (!ok) throw new Error("fallo");
  productos = datos;
  dibujarProductos();
}

// Trae la configuración. Siempre actualiza el cartel de abierto/cerrado.
// El formulario solo se llena cuando se pide (al entrar a la pestaña o al guardar), para no pisar lo que se está escribiendo.
async function cargarConfig(llenarFormulario = false) {
  const { ok, datos } = await api("/configuracion");
  if (!ok) throw new Error("fallo");
  config = datos;
  dibujarEstadoLocal();
  dibujarEstadoPromo();
  if (llenarFormulario) llenarFormularioConfig();
}

// Si llegó un pedido nuevo desde la última carga, suena. La primera carga no suena.
function avisarNuevos() {
  const ids = new Set(pedidos.filter(p => p.estado === "nuevo").map(p => p.id));
  if (idsNuevosVistos !== null && [...ids].some(id => !idsNuevosVistos.has(id))) pitar();
  idsNuevosVistos = ids;
}

// ---------- Sonido ----------
// Los navegadores solo dejan sonar después de que la persona toca algo en la página.
// Por eso hay un botón "Sonido": al tocarlo se habilita el audio.

function pitar() {
  if (!sonidoActivo || !audio) return;
  try {
    for (let i = 0; i < 2; i++) {
      const oscilador = audio.createOscillator();
      const volumen = audio.createGain();
      oscilador.frequency.value = 880;
      volumen.gain.value = 0.2;
      oscilador.connect(volumen);
      volumen.connect(audio.destination);
      const inicio = audio.currentTime + i * 0.35;
      oscilador.start(inicio);
      oscilador.stop(inicio + 0.2);
    }
  } catch { /* si el navegador no deja sonar, seguimos sin sonido */ }
}

function prepararAudio() {
  try {
    audio = audio || new (window.AudioContext || window.webkitAudioContext)();
    audio.resume();
  } catch { /* sin audio */ }
}

function alternarSonido() {
  sonidoActivo = !sonidoActivo;
  guardar("crave_sonido", sonidoActivo ? "1" : "0");
  if (sonidoActivo) {
    prepararAudio();
    pitar();   // un pitido de prueba
  }
  dibujarSonido();
}

function dibujarSonido() {
  const boton = $("boton-sonido");
  boton.textContent = sonidoActivo ? "Sonido: sí" : "Sonido: no";
  boton.setAttribute("aria-pressed", sonidoActivo ? "true" : "false");
  // Si la página se recargó con el sonido en "sí", el audio se habilita con el primer toque en cualquier lado
  if (sonidoActivo && !audio) document.addEventListener("click", prepararAudio, { once: true });
}

// ---------- Dibujar pedidos ----------

function dibujarPedidos() {
  const nuevos = pedidos.filter(p => p.estado === "nuevo").length;
  const activos = pedidos.filter(p => ESTADOS_ACTIVOS.includes(p.estado));
  const vendido = pedidos.filter(p => p.estado !== "cancelado").reduce((suma, p) => suma + p.total, 0);

  // Número rojo sobre la pestaña y en el título de la ventana
  $("globo-nuevos").hidden = nuevos === 0;
  $("globo-nuevos").textContent = nuevos;
  document.title = (nuevos > 0 ? "(" + nuevos + ") " : "") + "Panel CRAVE";

  $("resumen").innerHTML =
    '<div class="dato"><b>' + nuevos + '</b><span>Nuevos</span></div>' +
    '<div class="dato"><b>' + activos.length + '</b><span>En curso</span></div>' +
    '<div class="dato"><b>' + plata(vendido) + '</b><span>Vendido hoy</span></div>';

  const cuenta = (estado) => pedidos.filter(p => p.estado === estado).length;
  const sinPago = pedidos.filter(sinPagoConfirmado).length;
  const opciones = [
    ["activos", "En curso (" + activos.length + ")"],
    ["sinpago", "Sin pago (" + sinPago + ")"],
    ["entregado", "Entregados (" + cuenta("entregado") + ")"],
    ["cancelado", "Cancelados (" + cuenta("cancelado") + ")"],
    ["todos", "Todos (" + pedidos.length + ")"]
  ];
  $("filtros").innerHTML = opciones.map(([clave, texto]) =>
    // el botón "Sin pago" se marca en rojo mientras haya transferencias sin confirmar
    '<button type="button" class="filtro' + (clave === "sinpago" && sinPago > 0 ? ' alerta' : '') + '" data-accion="filtro" data-filtro="' + clave + '" aria-pressed="' + (filtro === clave) + '">' + texto + '</button>'
  ).join("");

  const visibles = pedidos.filter(p =>
    filtro === "todos" ? true :
    filtro === "activos" ? ESTADOS_ACTIVOS.includes(p.estado) :
    filtro === "sinpago" ? sinPagoConfirmado(p) :
    p.estado === filtro);

  $("lista-pedidos").innerHTML = visibles.length === 0
    ? '<p class="vacio">No hay pedidos en esta lista.</p>'
    : visibles.map(tarjetaPedido).join("");
}

function tarjetaPedido(p) {
  const items = p.items.map(i =>
    '<li><b>' + i.cantidad + ' ×</b> ' + esc(i.nombre) +
    (i.detalle ? '<span class="detalle">' + esc(i.detalle) + '</span>' : '') + '</li>'
  ).join("");

  // Teléfono: solo dígitos y "+", así el enlace tel: no puede llevar nada raro
  const telefonoLimpio = p.telefono.replace(/[^\d+]/g, "");
  const cliente = '<a href="tel:' + esc(telefonoLimpio) + '">' + esc(p.telefono) + '</a>';

  let entrega;
  if (p.modo === "delivery") {
    entrega = '<b>Delivery:</b> ' + esc(p.direccion) + (p.referencias ? ' (' + esc(p.referencias) + ')' : '');
  } else {
    entrega = '<b>Retiro</b> en el local';
  }

  const envio = p.envio > 0 ? '<div class="linea">Envío ' + plata(p.envio) + '</div>' : '';
  const nota = p.notas ? '<div class="nota"><b>Nota:</b> ' + esc(p.notas) + '</div>' : '';

  return '<article class="pedido ' + esc(p.estado) + ' pago-' + esc(p.estadoPago) + '">' +
    '<div class="pedido-cabecera"><span class="hora">' + esc(p.hora) + '</span>' +
    '<span><span class="numero">' + esc(p.numero) + ' · pedido a las ' + horaLocal(p.creadoEn) + '</span> ' +
    '<span class="badge ' + esc(p.estado) + '">' + esc(p.estado) + '</span></span></div>' +
    bloquePago(p) +
    '<div class="cliente">' + esc(p.nombre) + ' · ' + cliente + '</div>' +
    '<div class="linea">' + entrega + '</div>' +
    '<ul class="items">' + items + '</ul>' +
    nota + envio +
    '<div class="total"><span>Total</span><span>' + plata(p.total) + '</span></div>' +
    botonesPedido(p) +
    '</article>';
}

// "12 min" o "1 h 5 min"
function duracion(minutos) {
  if (minutos < 60) return minutos + " min";
  const resto = minutos % 60;
  return Math.floor(minutos / 60) + " h" + (resto > 0 ? " " + resto + " min" : "");
}

// El recuadro de pago de la tarjeta. En una transferencia sin confirmar es lo primero que tiene que saltar a la vista.
// Los textos salen de acá y de números del servidor (nada que haya escrito un cliente), por eso no pasan por esc().
function bloquePago(p) {
  const monto = plata(p.total);
  let titulo, detalle;

  if (p.estadoPago === "efectivo") {
    titulo = "Efectivo: cobra al entregar";
    detalle = p.abonaCon > 0 ? "Abona con " + plata(p.abonaCon) + ", vuelto " + plata(p.vuelto) : "Paga justo";
  } else if (p.estadoPago === "confirmado") {
    titulo = "Pago confirmado";
    detalle = "Transferencia recibida a las " + horaLocal(p.pagoConfirmadoEn);   // la hora ya termina en punto ("p. m.")
    if (p.estado === "cancelado") detalle += " El pedido está cancelado: hay que devolver la plata.";
  } else if (p.estadoPago === "demorado") {
    titulo = "Pago demorado";
    detalle = "Hace " + duracion(p.minutosEsperandoPago) + " que se espera la transferencia de " + monto + ". Revisá tu cuenta antes de preparar.";
  } else if (p.estadoPago === "sin_pago") {
    titulo = "Cancelado sin pago";
    detalle = "La transferencia nunca se confirmó.";
  } else {
    titulo = "Sin pago confirmado";
    detalle = "Esperando la transferencia de " + monto + " (hace " + duracion(p.minutosEsperandoPago) + ").";
  }

  return '<div class="pago ' + esc(p.estadoPago) + '"><b>' + titulo + '</b><span>' + detalle + '</span></div>';
}

function botonesPedido(p) {
  const botones = [];
  const siguiente = SIGUIENTE[p.estado];
  const sinPago = sinPagoConfirmado(p);

  // Transferencia sin confirmar: este botón va primero, porque lo primero es mirar tu cuenta y ver si llegó la plata
  if (sinPago) {
    botones.push('<button type="button" class="btn-estado pago-ok" data-accion="pago" data-id="' + p.id + '" data-confirmado="true">Transferencia recibida</button>');
  }

  if (siguiente) {
    // Empezar a preparar sin el pago confirmado pide un segundo toque. No se bloquea: el que decide sos vos.
    const esperaPreparar = sinPago && siguiente[0] === "preparando" && confirmando === "preparar:" + p.id;
    botones.push('<button type="button" class="btn-estado' + (esperaPreparar ? ' peligro' : '') + '" data-accion="estado" data-id="' + p.id + '" data-estado="' + siguiente[0] + '">' +
      (esperaPreparar ? 'Sin pago confirmado: tocá de nuevo para preparar' : siguiente[1]) + '</button>');
  }
  if (ANTERIOR[p.estado]) {
    botones.push('<button type="button" class="btn-estado secundario" data-accion="estado" data-id="' + p.id + '" data-estado="' + ANTERIOR[p.estado] + '">Volver a ' + ANTERIOR[p.estado] + '</button>');
  }
  // Por si se tocó "Transferencia recibida" sin querer
  if (p.estadoPago === "confirmado" && p.estado !== "cancelado") {
    botones.push('<button type="button" class="btn-estado secundario" data-accion="pago" data-id="' + p.id + '" data-confirmado="false">Deshacer pago confirmado</button>');
  }
  // Cancelar pide dos toques seguidos para evitar cancelar por error
  if (ESTADOS_ACTIVOS.includes(p.estado)) {
    const esperaCancelar = confirmando === "cancelar:" + p.id;
    const avisoCancelar = p.estadoPago === "confirmado"
      ? 'Ya pagó: tocá de nuevo para cancelar (hay que devolver la plata)'
      : 'Tocá de nuevo para cancelar';
    botones.push('<button type="button" class="btn-estado secundario' + (esperaCancelar ? ' peligro' : '') + '" data-accion="cancelar" data-id="' + p.id + '">' +
      (esperaCancelar ? avisoCancelar : 'Cancelar pedido') + '</button>');
  }
  return botones.length ? '<div class="acciones">' + botones.join("") + '</div>' : '';
}

// ---------- Estado del local y formulario de horarios ----------

let formularioConfigLleno = false;   // true una vez que el formulario ya tiene los valores del servidor

function dibujarEstadoLocal() {
  const caja = $("estado-local");
  caja.hidden = config === null;
  if (config === null) return;
  const abierto = config.localAbierto;
  caja.classList.toggle("cerrado", !abierto);
  caja.innerHTML =
    '<div><b>' + (abierto ? 'Local abierto' : 'Local cerrado') + '</b>' +
    '<span>' + (abierto ? 'Los clientes pueden hacer pedidos.' : 'Los clientes no pueden hacer pedidos.') + '</span></div>' +
    '<button type="button" class="btn-chico" data-accion="local" data-abierto="' + (!abierto) + '">' + (abierto ? 'Cerrar local' : 'Abrir local') + '</button>';
}

// Cartel de los precios de lanzamiento, debajo del de abierto/cerrado. Un toque los prende o los apaga.
// Prendidos: la tienda muestra el precio de antes tachado, la etiqueta de descuento y la cinta de lanzamiento,
// y el servidor cobra el precio de lanzamiento (columna precio_promo).
function dibujarEstadoPromo() {
  const caja = $("estado-promo");
  caja.hidden = config === null;
  if (config === null) return;
  const activa = config.promoActiva === true;
  caja.classList.toggle("activa", activa);
  caja.innerHTML =
    '<div><b>' + (activa ? 'Precios de lanzamiento activados' : 'Precios normales') + '</b>' +
    '<span>' + (activa ? 'Los clientes ven los descuentos y pagan el precio de lanzamiento.' : 'Los clientes pagan el precio normal. Los de lanzamiento están cargados, listos para activar.') + '</span></div>' +
    '<button type="button" class="btn-chico" data-accion="promo" data-activa="' + (!activa) + '">' + (activa ? 'Volver a precios normales' : 'Activar precios de lanzamiento') + '</button>';
}

function llenarFormularioConfig() {
  $("c-apertura").value = config.apertura;
  $("c-ultima").value = config.ultimaFranja;
  $("c-preparacion").value = config.minutosPreparacion;
  $("c-paso").value = config.minutosPorFranja;
  $("c-cupo").value = config.pedidosPorFranja;
  $("c-pago").value = config.minutosPagoDemorado;
  $("c-mensaje").value = config.mensajeCerrado || "";
  formularioConfigLleno = true;
}

$("form-config").addEventListener("submit", async (evento) => {
  evento.preventDefault();
  $("config-errores").innerHTML = "";
  $("config-ok").textContent = "";
  $("boton-config").disabled = true;
  try {
    const { ok, datos } = await api("/configuracion", "PUT", {
      apertura: $("c-apertura").value,
      ultimaFranja: $("c-ultima").value,
      minutosPreparacion: Number($("c-preparacion").value),
      minutosPorFranja: Number($("c-paso").value),
      pedidosPorFranja: Number($("c-cupo").value),
      minutosPagoDemorado: Number($("c-pago").value),
      mensajeCerrado: $("c-mensaje").value
    });
    if (ok) {
      await cargarConfig(true);   // volvemos a leer lo guardado para mostrar lo que quedó de verdad
      $("config-ok").textContent = "Guardado. Rige desde el próximo pedido.";
    } else {
      const lista = (datos && datos.errores) || ["No se pudo guardar. Probá de nuevo."];
      $("config-errores").innerHTML = lista.map(e => "<li>" + esc(e) + "</li>").join("");
    }
  } catch (error) {
    if (error.message !== "sin-sesion") $("config-errores").innerHTML = "<li>No hay conexión. Probá de nuevo.</li>";
  } finally {
    $("boton-config").disabled = false;
  }
});

// ---------- Dibujar productos ----------

// Debajo del nombre: precio normal y, si tiene, el de lanzamiento. Los tacos dicen "Próximamente".
function precioProducto(p) {
  let texto = plata(p.precio);
  if (p.precioPromo) texto += ' · lanzamiento ' + plata(p.precioPromo);
  if (p.proximamente) texto += ' · Próximamente (no se puede pedir)';
  return '<span class="producto-precio">' + texto + '</span>';
}

function dibujarProductos() {
  // Agrupamos por categoría respetando el orden en que llegan
  const grupos = new Map();
  productos.forEach(p => {
    if (!grupos.has(p.categoria)) grupos.set(p.categoria, []);
    grupos.get(p.categoria).push(p);
  });

  let html = "";
  grupos.forEach((lista, categoria) => {
    html += '<div class="grupo"><h2>' + esc(categoria) + '</h2>' + lista.map(p =>
      '<div class="producto' + (p.agotado ? ' agotado' : '') + '">' +
      '<span class="producto-nombre">' + esc(p.nombre) + precioProducto(p) + '</span>' +
      '<button type="button" class="interruptor" role="switch" aria-checked="' + p.agotado + '" aria-label="Agotado: ' + esc(p.nombre) + '" ' +
      'data-accion="agotado" data-id="' + esc(p.id) + '" data-agotado="' + (!p.agotado) + '">' + (p.agotado ? 'Agotado' : 'Disponible') + '</button>' +
      '</div>'
    ).join("") + '</div>';
  });
  $("lista-productos").innerHTML = html || '<p class="vacio">No hay productos.</p>';
}

// ---------- Acciones ----------

async function cambiarEstado(id, estado) {
  const { ok, datos } = await api("/pedidos/" + id + "/estado", "PATCH", { estado });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar el estado.");
  await cargarPedidos();   // siempre recargamos para mostrar lo que realmente quedó en la base
}

// Marca (confirmado = true) o desmarca "transferencia recibida"
async function cambiarPago(id, confirmado) {
  const { ok, datos } = await api("/pedidos/" + id + "/pago", "PATCH", { confirmado });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar el pago.");
  await cargarPedidos();
}

// Para acciones delicadas: el primer toque cambia el texto del botón y pide tocar de nuevo.
// Devuelve true si este toque es el segundo (hay que hacer la acción). Si no vuelve a tocar en 4 segundos, se olvida.
function pedirSegundoToque(accion, id) {
  const clave = accion + ":" + id;
  if (confirmando === clave) {
    confirmando = null;
    return true;
  }
  confirmando = clave;
  dibujarPedidos();
  setTimeout(() => { if (confirmando === clave) { confirmando = null; dibujarPedidos(); } }, 4000);
  return false;
}

async function cambiarAbierto(abierto) {
  const { ok, datos } = await api("/configuracion/abierto", "PATCH", { abierto });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar el estado del local.");
  await cargarConfig(false);
}

// Prende (activa = true) o apaga los precios de lanzamiento
async function cambiarPromo(activa) {
  const { ok, datos } = await api("/configuracion/promo", "PATCH", { activa });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar los precios.");
  await cargarConfig(false);
}

async function cambiarAgotado(id, agotado) {
  const { ok, datos } = await api("/productos/" + encodeURIComponent(id) + "/agotado", "PATCH", { agotado });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar el producto.");
  await cargarProductos();
}

function cambiarVista(nueva) {
  vista = nueva;
  $("vista-pedidos").hidden = vista !== "pedidos";
  $("vista-productos").hidden = vista !== "productos";
  $("vista-config").hidden = vista !== "config";
  if (vista === "config") { formularioConfigLleno = false; $("config-ok").textContent = ""; $("config-errores").innerHTML = ""; }
  document.querySelectorAll('[data-accion="vista"]').forEach(boton =>
    boton.setAttribute("aria-pressed", boton.dataset.vista === vista ? "true" : "false"));
  refrescar();
}

// Un solo "oyente" para todos los botones del panel: mira qué data-accion tiene el botón tocado.
$("panel").addEventListener("click", async (evento) => {
  const boton = evento.target.closest("[data-accion]");
  if (!boton || ocupado) return;
  const accion = boton.dataset.accion;

  try {
    if (accion === "vista") {
      cambiarVista(boton.dataset.vista);
    } else if (accion === "filtro") {
      filtro = boton.dataset.filtro;
      dibujarPedidos();
    } else if (accion === "actualizar") {
      mostrarAviso("");
      await refrescar();
    } else if (accion === "sonido") {
      alternarSonido();
    } else if (accion === "salir") {
      await fetch("/api/admin/logout", { method: "POST" });
      mostrarLogin();
    } else if (accion === "cancelar") {
      const id = Number(boton.dataset.id);
      if (pedirSegundoToque("cancelar", id)) {
        ocupado = true;
        mostrarAviso("");
        await cambiarEstado(id, "cancelado");
      }
    } else if (accion === "estado") {
      const id = Number(boton.dataset.id);
      const nuevo = boton.dataset.estado;
      const pedido = pedidos.find(p => p.id === id);
      // Empezar a preparar una transferencia sin pago confirmado: hace falta un segundo toque
      if (nuevo === "preparando" && pedido && sinPagoConfirmado(pedido) && !pedirSegundoToque("preparar", id)) return;
      confirmando = null;
      ocupado = true;
      mostrarAviso("");
      await cambiarEstado(id, nuevo);
    } else if (accion === "pago") {
      confirmando = null;
      ocupado = true;
      mostrarAviso("");
      await cambiarPago(Number(boton.dataset.id), boton.dataset.confirmado === "true");
    } else if (accion === "local") {
      ocupado = true;
      mostrarAviso("");
      await cambiarAbierto(boton.dataset.abierto === "true");
    } else if (accion === "promo") {
      ocupado = true;
      mostrarAviso("");
      await cambiarPromo(boton.dataset.activa === "true");
    } else if (accion === "agotado") {
      ocupado = true;
      mostrarAviso("");
      await cambiarAgotado(boton.dataset.id, boton.dataset.agotado === "true");
    }
  } catch (error) {
    if (error.message !== "sin-sesion") mostrarAviso("No hay conexión. Probá de nuevo.");
  } finally {
    ocupado = false;
  }
});

// Al volver a la pestaña, actualizamos enseguida
document.addEventListener("visibilitychange", () => {
  if (!document.hidden && !$("panel").hidden) refrescar();
});

revisarSesion();
