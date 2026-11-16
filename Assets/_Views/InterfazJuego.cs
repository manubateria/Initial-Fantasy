using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InterfazJuego : MonoBehaviour
{
    [Header("Controlador Principal")]
    public GameManager gameManager; // Necesario para enviar los clics narrativos
    [Header("Panel Táctico (Ficha)")]
    public GameObject panelTactico;

    [Header("Bloque 1 y 2: Identidad y Salud")]
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtNumVida;
    public Slider barraVida;
    public GameObject[] pipsAccion; // Array para meter los 3 puntitos azules

    [Header("Bloque 3: Atributos")]
    public TextMeshProUGUI txtDadoFUE;
    public TextMeshProUGUI txtDadoDES;
    public TextMeshProUGUI txtDadoINT;
    public TextMeshProUGUI txtDadoVOL;

    [Header("Bloques 4: Entorno e Inventario")]
    public TextMeshProUGUI txtUbicacionYPeligros;
    public TextMeshProUGUI txtInventario;

    [Header("Botones Tácticos")]
    public Button btnMover;
    public Button btnInteractuar;
    public Button btnAtacar;
    public Button btnPasarTurno;

    [Header("Panel Narrativo (Librojuego)")]
    public GameObject panelNarrativo;
    public TextMeshProUGUI txtTitulo;
    public TextMeshProUGUI txtCuerpoHistoria;
    public Button[] botonesNarrativos; // Asigna los 3 botones aquí
    public TextMeshProUGUI[] txtBotonesNarrativos; // Asigna los textos de los 3 botones aquí

    [Header("Registro de Combate")]
    public TextMeshProUGUI TextoResultado;

    public void MostrarDatos(Personaje pj)
    {
        // 1. Gestionar visibilidad de paneles
        panelNarrativo.SetActive(false);
        panelTactico.SetActive(true);
        // 2. Identidad y Estado
        string estadoMental = pj.EstaMuerto ? "MUERTO" : pj.EstadoPsicologico.ToString().ToUpper();
        txtNombre.text = pj.Nombre;
        txtEstado.text = $"ESTADO: {estadoMental}";

        // Cambiar color del texto de estado según gravedad
        if (pj.EstaMuerto) txtEstado.color = Color.red;
        else if (estadoMental == "SANO") txtEstado.color = Color.green;
        else txtEstado.color = new Color(1f, 0.5f, 0f); // Naranja para estrés/ansiedad

        // Lógica del Slider de Vida
        txtNumVida.text = $"PV: {pj.VidaActual}/{pj.VidaMaxima}";
        barraVida.maxValue = pj.VidaMaxima;
        barraVida.value = pj.VidaActual;

        // 2.5 Sistema de Puntos de Acción (Pips visuales)
        for (int i = 0; i < pipsAccion.Length; i++)
        {
            // Activa el puntito azul solo si el índice es menor que las acciones actuales
            pipsAccion[i].SetActive(i < pj.AccionesActuales);
        }

        // Bloque 3: Atributos (Leyendo tu diccionario de Atributos)
        txtDadoFUE.text = pj.Atributos.ContainsKey("FUE") ? $"d{pj.Atributos["FUE"]}" : "-";
        txtDadoDES.text = pj.Atributos.ContainsKey("DES") ? $"d{pj.Atributos["DES"]}" : "-";
        txtDadoINT.text = pj.Atributos.ContainsKey("INT") ? $"d{pj.Atributos["INT"]}" : "-";
        txtDadoVOL.text = pj.Atributos.ContainsKey("VOL") ? $"d{pj.Atributos["VOL"]}" : "-";

        // 3. Entorno y Peligros
        string ubicacion = $"SALA: {pj.NodoActual.Nombre}\n";
        if (pj.NodoActual.Estados.Count > 0)
        {
            ubicacion += "PELIGROS: ";
            foreach (EstadoEntorno est in pj.NodoActual.Estados) ubicacion += $"<color=orange>[{est.ToString().ToUpper()}]</color> ";
            ubicacion += "\n";
        }

        ubicacion += "CONEXIONES: ";
        foreach (Nodo n in pj.NodoActual.Adyacentes) ubicacion += n.Descubierto ? $"{n.Nombre}, " : "???, ";

        // 4. Puntos de Interés
        if (pj.NodoActual.PuntosDeInteres.Count > 0)
        {
            ubicacion += "\nPOIs: ";
            foreach (PuntoInteres poi in pj.NodoActual.PuntosDeInteres)
            {
                string estadoPOI = poi.YaInteractuado ? "<color=gray>(Explorado)</color>" : "<color=green>(Activo)</color>";
                ubicacion += $"{poi.Nombre} {estadoPOI}\n";
            }
        }
        txtUbicacionYPeligros.text = ubicacion;

        // 5. Inventario Básico
        string inv = "MANO DCHA: ";
        if (pj.Inventario.ContainsKey(SlotEquipamiento.ManoDerecha))
        {
            inv += $"{pj.Inventario[SlotEquipamiento.ManoDerecha].Nombre}\n";
        }
        else inv += "Vacía\n";

        inv += "EQUIPO EXTRA:\n";
        foreach (Item equipo in pj.EquipamientoAdicional) inv += $"- {equipo.Nombre}\n";
        txtInventario.text = inv;

        // 6. Lógica Dinámica de Botones
        bool tieneAcciones = pj.AccionesActuales > 0;

        // Mover: Requiere PA y que haya salas conectadas
        btnMover.interactable = tieneAcciones && pj.NodoActual.Adyacentes.Count > 0;

        // Interactuar: Requiere PA y que exista al menos un POI sin explorar en la sala
        btnInteractuar.interactable = tieneAcciones && pj.NodoActual.PuntosDeInteres.Exists(p => !p.YaInteractuado);

        // Atacar: Requiere PA (más adelante le añadiremos la comprobación de si hay monstruos)
        btnAtacar.interactable = tieneAcciones;

        // Pasar turno: Siempre disponible para no bloquear la partida
        btnPasarTurno.interactable = true;
    }

    public void MostrarRegistroCombate(string mensaje)
    {
        TextoResultado.text = mensaje;
    }

    // ==========================================
    // MODO NARRATIVO (Librojuego)
    // ==========================================
    public void MostrarPaginaNarrativa(Personaje pj, string tituloEvento, PaginaNarrativaDTO pagina, string idEventoActual)
    {
        // 1. Gestionar visibilidad de paneles
        panelTactico.SetActive(false);
        panelNarrativo.SetActive(true);

        txtTitulo.text = tituloEvento.ToUpper();
        txtCuerpoHistoria.text = pagina.TextoDescriptivo;

        // 2. Limpiar y configurar botones
        for (int i = 0; i < botonesNarrativos.Length; i++)
        {
            botonesNarrativos[i].gameObject.SetActive(false); // Ocultar por defecto
            botonesNarrativos[i].onClick.RemoveAllListeners(); // Limpiar eventos anteriores
            botonesNarrativos[i].interactable = true; // Activar interactividad
        }

        // 3. Pintar opciones
        for (int i = 0; i < pagina.Opciones.Count && i < botonesNarrativos.Length; i++)
        {
            OpcionNarrativaDTO opt = pagina.Opciones[i];
            botonesNarrativos[i].gameObject.SetActive(true);

            // Evaluar bloqueos por atributos
            bool bloqueadaPorAtributo = false;
            if (!string.IsNullOrEmpty(opt.RequiereAtributo))
            {
                if (!pj.Atributos.ContainsKey(opt.RequiereAtributo) || (int)pj.Atributos[opt.RequiereAtributo] < opt.NivelDadoRequerido)
                {
                    bloqueadaPorAtributo = true;
                }
            }

            if (bloqueadaPorAtributo)
            {
                txtBotonesNarrativos[i].text = $"<color=grey>[BLOQUEADA] {opt.TextoBoton} (Req. d{opt.NivelDadoRequerido} en {opt.RequiereAtributo})</color>";
                botonesNarrativos[i].interactable = false; // El botón físico no se puede pulsar
            }
            else
            {
                string infoTirada = string.IsNullOrEmpty(opt.AtributoTirada) ? "" : $" <color=red>[Tirada: {opt.AtributoTirada} DC {opt.DificultadDC}]</color>";
                txtBotonesNarrativos[i].text = $"{opt.TextoBoton}{infoTirada}";

                // INYECCIÓN DE EVENTO: Conectar el clic del botón al GameManager
                OpcionNarrativaDTO optLocal = opt; // Evitar problema de variable capturada en bucle (closure)
                botonesNarrativos[i].onClick.AddListener(() => gameManager.ProcesarOpcionNarrativa(optLocal, idEventoActual));
            }
        }
    }
}