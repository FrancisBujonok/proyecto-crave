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

const $ = (id) => document.getElementById(id);

// ---------- Estado de la pantalla ----------
let pedidos = [];
let productos = [];
let vista = "pedidos";        // "pedidos" o "productos"
let filtro = "activos";       // "activos", "entregado", "cancelado" o "todos"
let cancelando = null;        // id del pedido que espera el segundo toque para cancelarse
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
    if (vista === "pedidos") await cargarPedidos(); else await cargarProductos();
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
  const opciones = [
    ["activos", "En curso (" + activos.length + ")"],
    ["entregado", "Entregados (" + cuenta("entregado") + ")"],
    ["cancelado", "Cancelados (" + cuenta("cancelado") + ")"],
    ["todos", "Todos (" + pedidos.length + ")"]
  ];
  $("filtros").innerHTML = opciones.map(([clave, texto]) =>
    '<button type="button" class="filtro" data-accion="filtro" data-filtro="' + clave + '" aria-pressed="' + (filtro === clave) + '">' + texto + '</button>'
  ).join("");

  const visibles = pedidos.filter(p =>
    filtro === "todos" ? true : filtro === "activos" ? ESTADOS_ACTIVOS.includes(p.estado) : p.estado === filtro);

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

  let pago;
  if (p.pago === "transferencia") {
    pago = '<b>Transferencia:</b> confirmar que llegó';
  } else if (p.abonaCon > 0) {
    pago = '<b>Efectivo:</b> abona con ' + plata(p.abonaCon) + ', vuelto ' + plata(p.vuelto);
  } else {
    pago = '<b>Efectivo</b>';
  }

  const envio = p.envio > 0 ? '<div class="linea">Envío ' + plata(p.envio) + '</div>' : '';
  const nota = p.notas ? '<div class="nota"><b>Nota:</b> ' + esc(p.notas) + '</div>' : '';

  return '<article class="pedido ' + esc(p.estado) + '">' +
    '<div class="pedido-cabecera"><span class="hora">' + esc(p.hora) + '</span>' +
    '<span><span class="numero">' + esc(p.numero) + ' · pedido a las ' + horaLocal(p.creadoEn) + '</span> ' +
    '<span class="badge ' + esc(p.estado) + '">' + esc(p.estado) + '</span></span></div>' +
    '<div class="cliente">' + esc(p.nombre) + ' · ' + cliente + '</div>' +
    '<div class="linea">' + entrega + '</div>' +
    '<ul class="items">' + items + '</ul>' +
    nota +
    '<div class="linea">' + pago + '</div>' + envio +
    '<div class="total"><span>Total</span><span>' + plata(p.total) + '</span></div>' +
    botonesPedido(p) +
    '</article>';
}

function botonesPedido(p) {
  const botones = [];
  const siguiente = SIGUIENTE[p.estado];

  if (siguiente) {
    botones.push('<button type="button" class="btn-estado" data-accion="estado" data-id="' + p.id + '" data-estado="' + siguiente[0] + '">' + siguiente[1] + '</button>');
  }
  if (ANTERIOR[p.estado]) {
    botones.push('<button type="button" class="btn-estado secundario" data-accion="estado" data-id="' + p.id + '" data-estado="' + ANTERIOR[p.estado] + '">Volver a ' + ANTERIOR[p.estado] + '</button>');
  }
  // Cancelar pide dos toques seguidos para evitar cancelar por error
  if (ESTADOS_ACTIVOS.includes(p.estado)) {
    const confirmando = cancelando === p.id;
    botones.push('<button type="button" class="btn-estado secundario' + (confirmando ? ' peligro' : '') + '" data-accion="cancelar" data-id="' + p.id + '">' +
      (confirmando ? 'Tocá de nuevo para cancelar' : 'Cancelar pedido') + '</button>');
  }
  return botones.length ? '<div class="acciones">' + botones.join("") + '</div>' : '';
}

// ---------- Dibujar productos ----------

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
      '<span class="producto-nombre">' + esc(p.nombre) + '<span class="producto-precio">' + plata(p.precio) + '</span></span>' +
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

async function cambiarAgotado(id, agotado) {
  const { ok, datos } = await api("/productos/" + encodeURIComponent(id) + "/agotado", "PATCH", { agotado });
  if (!ok) mostrarAviso((datos && datos.error) || "No se pudo cambiar el producto.");
  await cargarProductos();
}

function cambiarVista(nueva) {
  vista = nueva;
  $("vista-pedidos").hidden = vista !== "pedidos";
  $("vista-productos").hidden = vista !== "productos";
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
      if (cancelando !== id) {
        // primer toque: pedimos confirmación y la borramos a los 4 segundos si no toca de nuevo
        cancelando = id;
        dibujarPedidos();
        setTimeout(() => { if (cancelando === id) { cancelando = null; dibujarPedidos(); } }, 4000);
      } else {
        cancelando = null;
        ocupado = true;
        mostrarAviso("");
        await cambiarEstado(id, "cancelado");
      }
    } else if (accion === "estado") {
      cancelando = null;
      ocupado = true;
      mostrarAviso("");
      await cambiarEstado(Number(boton.dataset.id), boton.dataset.estado);
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
