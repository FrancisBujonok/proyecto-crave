// Pantalla de entrada del panel. Habla con /api/admin/login, /api/admin/yo y /api/admin/logout.

const formLogin = document.getElementById("form-login");
const panel = document.getElementById("panel");
const campoPassword = document.getElementById("password");
const textoError = document.getElementById("error");
const botonEntrar = document.getElementById("boton-entrar");
const botonSalir = document.getElementById("boton-salir");

// Muestra una pantalla u otra según haya sesión o no.
function mostrar(haySesion) {
  panel.hidden = !haySesion;
  formLogin.hidden = haySesion;
  if (!haySesion) campoPassword.focus();
}

// Al abrir la página: preguntamos al servidor si la cookie de sesión sigue valiendo.
async function revisarSesion() {
  try {
    const respuesta = await fetch("/api/admin/yo");
    mostrar(respuesta.ok);
  } catch {
    mostrar(false);
  }
}

formLogin.addEventListener("submit", async (evento) => {
  evento.preventDefault();   // evita que el navegador recargue la página
  textoError.textContent = "";
  botonEntrar.disabled = true;

  try {
    const respuesta = await fetch("/api/admin/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ password: campoPassword.value })
    });

    if (respuesta.ok) {
      campoPassword.value = "";
      mostrar(true);
    } else {
      // El servidor manda { error: "..." } con el motivo
      const datos = await respuesta.json().catch(() => ({}));
      textoError.textContent = datos.error || "No se pudo entrar. Probá de nuevo.";
    }
  } catch {
    textoError.textContent = "No hay conexión con el servidor.";
  } finally {
    botonEntrar.disabled = false;
  }
});

botonSalir.addEventListener("click", async () => {
  await fetch("/api/admin/logout", { method: "POST" });
  mostrar(false);
});

revisarSesion();
