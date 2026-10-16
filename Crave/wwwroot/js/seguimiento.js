// Página de seguimiento del pedido (seguimiento.html).
// El cliente llega con un enlace /seguimiento.html?c=CODIGO. Le preguntamos al servidor por ese código
// cada 15 segundos y mostramos en qué estado está su pedido. Cuando se entrega o se cancela dejamos de preguntar.
// El servidor solo manda lo mínimo (número, estado, horario, modo, pago y total): nada de datos personales.

const CADA_MS = 15000;          // se vuelve a preguntar cada 15 segundos
const ESPERA_LIMITE_MS = 60000; // si el servidor dice "demasiadas consultas" (429), esperamos un minuto
const ZONA_HORARIA = "America/Argentina/Buenos_Aires";

const $ = (id) => document.getElementById(id);
const dinero = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS", maximumFractionDigits: 0 });

// Los cuatro pasos del recorrido normal. "cancelado" no es un paso: se muestra aparte.
const PASOS = [
  ["nuevo", "Recibido"],
  ["preparando", "Preparando"],
  ["listo", "Listo"],
  ["entregado", "Entregado"]
];
const ESTADOS_FINALES = ["entregado", "cancelado"];

const codigo = (new URLSearchParams(location.search).get("c") || "").trim();
let temporizador = null;
let detenido = false;   // true cuando ya no hay nada más que preguntar (entregado, cancelado o enlace que no sirve)

// Todo texto que viene del servidor se pone con textContent (nunca con innerHTML): así no se puede colar código.

// Qué se le dice al cliente en cada estado: [título, detalle]
function mensajeDe(p) {
  const delivery = p.modo === "delivery";
  switch (p.estado) {
    case "nuevo":
      return ["Recibimos tu pedido", "En breve lo empezamos a preparar."];
    case "preparando":
      return ["Estamos preparando tu pedido", "Tu horario es a las " + p.hora + " hs."];
    case "listo":
      return delivery
        ? ["Tu pedido está listo", "Sale hacia tu domicilio."]
        : ["Tu pedido está listo para retirar", "Te esperamos en el local."];
    case "entregado":
      return ["Pedido entregado", "¡Gracias por pedir en CRAVE!"];
    case "cancelado":
      return ["Pedido cancelado", "Si no lo esperabas, o si ya hiciste la transferencia, escribinos por el chat de WhatsApp donde enviaste el pedido."];
    default:
      return ["Tu pedido está en marcha", "Volvé a mirar en unos minutos."];
  }
}

// La línea "Pago" de la tarjeta de datos
function textoPago(p) {
  if (p.pago === "transferencia") {
    if (p.pagoConfirmado) return "Transferencia recibida";
    return p.estado === "cancelado" ? "Transferencia sin acreditar" : "Transferencia: falta confirmarla";
  }
  return "Efectivo " + (p.modo === "delivery" ? "al recibirlo" : "al retirar");
}

function dibujarPasos(p) {
  const lista = $("pasos");
  lista.replaceChildren();
  lista.hidden = p.estado === "cancelado";   // un pedido cancelado no avanza
  if (p.estado === "cancelado") return;

  const actual = PASOS.findIndex(([estado]) => estado === p.estado);
  PASOS.forEach(([, nombre], i) => {
    const hecho = i < actual || p.estado === "entregado";   // al entregar, todos quedan completos
    const esActual = i === actual;
    const paso = document.createElement("li");
    paso.className = "paso" + (hecho ? " hecho" : "") + (esActual ? " actual" : "");
    if (esActual) paso.setAttribute("aria-current", "step");

    const marca = document.createElement("span");
    marca.className = "marca-paso";
    marca.setAttribute("aria-hidden", "true");
    marca.textContent = hecho ? "✓" : String(i + 1);

    const texto = document.createElement("span");
    texto.className = "nombre-paso";
    texto.textContent = nombre;

    paso.append(marca, texto);
    lista.append(paso);
  });
}

function dibujarDatos(p) {
  const filas = [
    ["Horario", p.hora + " hs"],
    ["Entrega", p.modo === "delivery" ? "Delivery" : "Retiro en el local"],
    ["Pago", textoPago(p)],
    ["Total", dinero.format(p.total)]
  ];
  const lista = $("datos");
  lista.replaceChildren();
  filas.forEach(([nombre, valor]) => {
    const dt = document.createElement("dt");
    dt.textContent = nombre;
    const dd = document.createElement("dd");
    dd.textContent = valor;
    lista.append(dt, dd);
  });
}

function dibujarPedido(p) {
  const [titulo, detalle] = mensajeDe(p);
  $("numero").textContent = p.numero;
  $("titulo").textContent = titulo;
  $("detalle").textContent = detalle;
  $("pedido").className = p.estado === "cancelado" ? "cancelado" : "";
  document.title = titulo + " | CRAVE";

  // Transferencia sin confirmar en un pedido que sigue en marcha: se lo recordamos
  const falta = p.pago === "transferencia" && !p.pagoConfirmado && !ESTADOS_FINALES.includes(p.estado);
  $("aviso-pago").hidden = !falta;
  if (falta) $("aviso-pago").textContent = "Todavía no vimos llegar tu transferencia. Mandanos el comprobante por el chat de WhatsApp donde enviaste el pedido.";

  dibujarPasos(p);
  dibujarDatos(p);

  $("cargando").hidden = true;
  $("error").hidden = true;
  $("pedido").hidden = false;
}

function dibujarError(titulo, texto) {
  $("cargando").hidden = true;
  $("pedido").hidden = true;
  $("error-titulo").textContent = titulo;
  $("error-texto").textContent = texto;
  $("error").hidden = false;
  $("actualizado").textContent = "";
  document.title = titulo + " | CRAVE";
}

function programar(ms) {
  clearTimeout(temporizador);
  temporizador = setTimeout(cargar, ms);
}

// Le pregunta el estado al servidor y decide cuándo volver a preguntar.
async function cargar() {
  if (document.hidden) { programar(CADA_MS); return; }   // con la pestaña en segundo plano no gastamos datos

  try {
    const respuesta = await fetch("/api/seguimiento/" + encodeURIComponent(codigo));

    if (respuesta.status === 404) {
      dibujarError("No encontramos tu pedido", "Revisá que el enlace esté completo o escribinos por el chat de WhatsApp donde enviaste el pedido.");
      detenido = true;   // no tiene sentido volver a preguntar
      return;
    }
    if (respuesta.status === 429) {
      $("actualizado").textContent = "Hiciste muchas consultas seguidas. Esperá un momento.";
      programar(ESPERA_LIMITE_MS);
      return;
    }
    if (!respuesta.ok) throw new Error("fallo");

    const pedido = await respuesta.json();
    dibujarPedido(pedido);
    if (ESTADOS_FINALES.includes(pedido.estado)) {
      $("actualizado").textContent = "";   // ya no cambia: no hace falta decir cuándo se actualizó
      detenido = true;
      return;
    }
    $("actualizado").textContent = "Se actualiza sola. Última vez: " +
      new Date().toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit", timeZone: ZONA_HORARIA });   // la hora ya termina en punto ("p. m.")
  } catch {
    // sin internet o el servidor se está despertando: mantenemos lo que ya se veía y reintentamos
    $("actualizado").textContent = "Sin conexión. Reintentando…";
  }
  programar(CADA_MS);
}

// Al volver a la pestaña, actualizamos enseguida
document.addEventListener("visibilitychange", () => {
  if (!document.hidden && !detenido && codigo !== "") cargar();
});

if (codigo === "") {
  dibujarError("Falta el código del pedido", "Entrá desde el enlace que te dimos al confirmar el pedido.");
  detenido = true;
} else {
  cargar();
}
