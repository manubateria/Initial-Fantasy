using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ==========================================
// MÁQUINA DE ESTADOS
// ==========================================
public enum FaseJuego
{
    Mantenimiento,
    Jugadores,
    Eventos
}

public class GameManager : MonoBehaviour
{
    [Header("Conexiones UI")]
    public InterfazJuego pantallaUI;

    // --- MEMORIA Y ESTADO ---
    private List<Personaje> listaPersonajes = new List<Personaje>();
    private int indiceActual = 0;

    // CAMBIO: Sustituimos el monstruo único por una lista
    private List<Enemigo> listaEnemigos = new List<Enemigo>();
    public FaseJuego faseActual;

    // --- BASES DE DATOS EN MEMORIA (JSON) ---
    private Dictionary<string, ItemDTO> catalogoItems = new Dictionary<string, ItemDTO>();
    private Dictionary<string, EnemigoDTO> catalogoEnemigos = new Dictionary<string, EnemigoDTO>();

    void Start()
    {
        // 0. CARGA DE CATÁLOGOS JSON (La base de datos arranca primero)
        CargarCatalogos();

        // 1. CREACIÓN DEL MAPA (GRAFO ESPACIAL)
        Nodo pasillo = new Nodo("Pasillo Oscuro");
        Nodo biblioteca = new Nodo("Biblioteca Oculta");
        pasillo.ConectarCon(biblioteca); // La puerta que une ambos

        // Creamos un objeto de botín leyendo del catálogo
        Item documentos = CrearItemDesdeCatalogo("KEY_PLANOS");

        // Creamos el POI con el botín dentro
        PuntoInteres cajaFuerte = new PuntoInteres(
            "Caja Fuerte Oxidada",
            "Una caja fuerte empotrada tras una estantería.",
            "Destreza",
            5,
            "¡Has forzado la cerradura y obtenido documentos confidenciales!",
            documentos
        );
        biblioteca.AgregarPOI(cajaFuerte);

        // 2. LECTURA Y CREACIÓN DE PERSONAJES DESDE JSON
        string rutaArchivo = Path.Combine(Application.dataPath, "_Data", "plantillas_personajes.json");
        if (!File.Exists(rutaArchivo))
        {
            Debug.LogError("No se encuentra el JSON en: " + rutaArchivo);
            return;
        }

        string jsonCrudo = File.ReadAllText(rutaArchivo);
        ContenedorPlantillasDTO contenedor = JsonUtility.FromJson<ContenedorPlantillasDTO>(jsonCrudo);

        foreach (PlantillaPersonajeDTO plantillaDTO in contenedor.Plantillas)
        {
            Personaje nuevoPersonaje = new Personaje(plantillaDTO.NombrePlantilla, plantillaDTO.VidaBase);

            foreach (AtributoDTO atributoDTO in plantillaDTO.AtributosIniciales)
            {
                TipoDado dadoAsignado = (TipoDado)atributoDTO.CarasDado;
                nuevoPersonaje.AsignarAtributo(atributoDTO.NombreAtributo, dadoAsignado);
            }

            // EQUIPAMIENTO INICIAL DESDE CATÁLOGO
            Item armaInicial = CrearItemDesdeCatalogo("WPN_REVOLVER");
            Item amuleto = CrearItemDesdeCatalogo("UTL_AMULETO");

            if (armaInicial != null) nuevoPersonaje.EquiparItem(armaInicial);
            if (amuleto != null) nuevoPersonaje.EquiparItem(amuleto);

            nuevoPersonaje.EstablecerUbicacionInicial(pasillo);

            listaPersonajes.Add(nuevoPersonaje);
        }

        // 3. INICIALIZACIÓN DE LA INTERFAZ
        if (listaPersonajes.Count > 0)
        {
            pantallaUI.MostrarDatos(listaPersonajes[0]);
        }

        // 4. GENERACIÓN DE ENEMIGOS DESDE CATÁLOGO
        if (catalogoEnemigos.ContainsKey("MNST_HIBRIDO"))
        {
            EnemigoDTO datos = catalogoEnemigos["MNST_HIBRIDO"];

            // Creamos el Monstruo A en el Pasillo
            Enemigo monstruoA = new Enemigo($"{datos.Nombre} A", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque);
            monstruoA.EstablecerUbicacionInicial(pasillo);
            listaEnemigos.Add(monstruoA);

            // Creamos el Monstruo B en la Biblioteca
            Enemigo monstruoB = new Enemigo($"{datos.Nombre} B", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque);
            monstruoB.EstablecerUbicacionInicial(biblioteca);
            listaEnemigos.Add(monstruoB);
        }

        // 5. ARRANQUE DE LA SECUENCIA
        CambiarFase(FaseJuego.Mantenimiento);
    }

    // ==========================================
    // LECTURA DE CATÁLOGOS JSON
    // ==========================================
    private void CargarCatalogos()
    {
        // 1. Cargar Items
        string rutaItems = Path.Combine(Application.dataPath, "_Data", "items.json");
        if (File.Exists(rutaItems))
        {
            string json = File.ReadAllText(rutaItems);
            CatalogoItemsDTO catItems = JsonUtility.FromJson<CatalogoItemsDTO>(json);
            foreach (ItemDTO i in catItems.Items)
            {
                if (!catalogoItems.ContainsKey(i.ID)) catalogoItems.Add(i.ID, i);
            }
        }
        else
        {
            Debug.LogWarning("Archivo items.json no encontrado.");
        }

        // 2. Cargar Monstruos
        string rutaMonstruos = Path.Combine(Application.dataPath, "_Data", "monstruos.json");
        if (File.Exists(rutaMonstruos))
        {
            string json = File.ReadAllText(rutaMonstruos);
            CatalogoEnemigosDTO catMonstruos = JsonUtility.FromJson<CatalogoEnemigosDTO>(json);
            foreach (EnemigoDTO e in catMonstruos.Monstruos)
            {
                if (!catalogoEnemigos.ContainsKey(e.ID)) catalogoEnemigos.Add(e.ID, e);
            }
        }
        else
        {
            Debug.LogWarning("Archivo monstruos.json no encontrado.");
        }
    }

    // Factoría para instanciar Items
    public Item CrearItemDesdeCatalogo(string id)
    {
        if (catalogoItems.ContainsKey(id))
        {
            ItemDTO dto = catalogoItems[id];
            return new Item(dto.Nombre, (CategoriaItem)dto.Categoria, (SlotEquipamiento)dto.SlotRequerido, dto.AtributoUso, dto.ValorEfecto);
        }
        Debug.LogError("Item no encontrado en catálogo: " + id);
        return null;
    }

    // ==========================================
    // CEREBRO DEL JUEGO (CONTROL DE FASES)
    // ==========================================

    public void CambiarFase(FaseJuego nuevaFase)
    {
        faseActual = nuevaFase;

        switch (faseActual)
        {
            case FaseJuego.Mantenimiento:
                pantallaUI.MostrarRegistroCombate("--- FASE DE MANTENIMIENTO ---\nRestaurando puntos de acción...");

                // Recargamos las acciones de todos los investigadores
                foreach (Personaje pj in listaPersonajes)
                {
                    pj.RestaurarAcciones();
                }

                pantallaUI.MostrarDatos(listaPersonajes[indiceActual]); // Refrescar pantalla
                Invoke("PasarAJugadores", 1.5f);
                break;

            case FaseJuego.Jugadores:
                pantallaUI.MostrarRegistroCombate("--- FASE DE JUGADORES ---\nEs tu turno. Elige tus acciones.");
                break;

            case FaseJuego.Eventos:
                pantallaUI.MostrarRegistroCombate("--- FASE DE EVENTOS ---\nLos monstruos actúan...");
                EjecutarTurnoEnemigos();
                break;
        }
    }

    private void PasarAJugadores()
    {
        CambiarFase(FaseJuego.Jugadores);
    }

    private void ReiniciarRonda()
    {
        CambiarFase(FaseJuego.Mantenimiento);
    }

    // ==========================================
    // CONTROLES DE LA INTERFAZ (BOTONES)
    // ==========================================

    public void SiguientePersonaje()
    {
        indiceActual++;
        if (indiceActual >= listaPersonajes.Count) indiceActual = 0;
        pantallaUI.MostrarDatos(listaPersonajes[indiceActual]);
    }

    public void AnteriorPersonaje()
    {
        indiceActual--;
        if (indiceActual < 0) indiceActual = listaPersonajes.Count - 1;
        pantallaUI.MostrarDatos(listaPersonajes[indiceActual]);
    }

    public void TerminarTurnoJugador()
    {
        if (faseActual == FaseJuego.Jugadores)
        {
            CambiarFase(FaseJuego.Eventos);
        }
    }

    // --- ACCIÓN: ATACAR ---
    public void TirarDadoPrueba()
    {
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];

        if (pjActual.EstaMuerto)
        {
            pantallaUI.MostrarRegistroCombate($"<color=red>{pjActual.Nombre} está muerto.</color> No puede atacar.");
            return;
        }

        // NUEVO: Auto-Targeting. Buscamos el primer enemigo vivo en nuestra misma sala.
        Enemigo objetivo = null;
        foreach (Enemigo e in listaEnemigos)
        {
            if (!e.EstaMuerto && e.NodoActual == pjActual.NodoActual)
            {
                objetivo = e;
                break; // Encontramos objetivo, paramos de buscar
            }
        }

        if (objetivo == null)
        {
            pantallaUI.MostrarRegistroCombate("No hay monstruos aquí. ¡Guarda munición!");
            return;
        }

        if (!pjActual.GastarAccion())
        {
            pantallaUI.MostrarRegistroCombate($"<color=red>¡Sin energía!</color> {pjActual.Nombre} no tiene acciones restantes.");
            return;
        }

        string atributoAUsar = "Fuerza";
        int danoAAplicar = 1;
        string nombreAtaque = "Puños";

        if (pjActual.Inventario.ContainsKey(SlotEquipamiento.ManoDerecha))
        {
            Item itemMano = pjActual.Inventario[SlotEquipamiento.ManoDerecha];
            if (itemMano.Categoria == CategoriaItem.Arma)
            {
                atributoAUsar = itemMano.AtributoUso;
                danoAAplicar = itemMano.ValorEfecto;
                nombreAtaque = itemMano.Nombre;
            }
        }

        int resultadoTirada = pjActual.RealizarTirada(atributoAUsar);
        string registro = $"{pjActual.Nombre} ataca al {objetivo.Nombre} con {nombreAtaque}...\n";
        registro += $"Tirada de {atributoAUsar}: {resultadoTirada} (Dificultad: {objetivo.DefensaDC})\n";

        if (resultadoTirada >= objetivo.DefensaDC)
        {
            objetivo.RecibirDano(danoAAplicar);
            registro += $"<color=green>¡ÉXITO!</color> El enemigo recibe {danoAAplicar} de daño.\n";

            if (objetivo.EstaMuerto) registro += $"\n<color=yellow>¡Has matado al {objetivo.Nombre}!</color>";
            else registro += $"Vida del enemigo: {objetivo.VidaActual} / {objetivo.VidaMaxima}\n";
        }
        else
        {
            registro += "<color=red>¡FALLO!</color> El ataque no surte efecto.";
        }

        pantallaUI.MostrarRegistroCombate(registro);
        pantallaUI.MostrarDatos(pjActual);
    }

    // --- ACCIÓN: MOVERSE ---
    public void BotonMoverPrueba()
    {
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        // Si no hay nodos a los que moverse (no debería pasar, pero por seguridad)
        if (pjActual.NodoActual.Adyacentes.Count == 0) return;

        // Comprobamos Puntos de Acción
        if (!pjActual.GastarAccion())
        {
            pantallaUI.MostrarRegistroCombate($"<color=red>¡Sin energía!</color> No puedes moverte más este turno.");
            return;
        }

        // Mapeamos el destino (primera conexión disponible)
        Nodo destino = pjActual.NodoActual.Adyacentes[0];

        // Efectuamos el movimiento
        pjActual.CambiarDeNodo(destino);

        pantallaUI.MostrarRegistroCombate($"{pjActual.Nombre} camina hacia: {destino.Nombre}.");
        pantallaUI.MostrarDatos(pjActual);
    }

    // --- ACCIÓN: INTERACTUAR CON POI ---
    public void BotonInteractuarPrueba()
    {
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        // Comprobamos si hay POIs en la sala actual
        if (pjActual.NodoActual.PuntosDeInteres.Count == 0)
        {
            pantallaUI.MostrarRegistroCombate("No hay nada de interés con qué interactuar en esta habitación.");
            return;
        }

        // Buscamos el primer POI que no haya sido explorado aún
        PuntoInteres poiActual = null;
        foreach (var p in pjActual.NodoActual.PuntosDeInteres)
        {
            if (!p.YaInteractuado)
            {
                poiActual = p;
                break;
            }
        }

        if (poiActual == null)
        {
            pantallaUI.MostrarRegistroCombate("Los puntos de interés de esta sala ya han sido investigados.");
            return;
        }

        // Comprobamos Puntos de Acción
        if (!pjActual.GastarAccion())
        {
            pantallaUI.MostrarRegistroCombate($"<color=red>¡Sin energía!</color> No tienes acciones para investigar.");
            return;
        }

        // Ejecutamos la tirada basada en el atributo que exija el POI
        int resultadoTirada = pjActual.RealizarTirada(poiActual.AtributoRequerido);

        string registro = $"{pjActual.Nombre} investiga '{poiActual.Nombre}'...\n";
        registro += $"Tirada de {poiActual.AtributoRequerido}: {resultadoTirada} (Dificultad DC: {poiActual.DificultadDC})\n";

        if (resultadoTirada >= poiActual.DificultadDC)
        {
            poiActual.MarcarComoInteractuado();
            registro += $"<color=green>¡ÉXITO!</color> {poiActual.RecompensaTexto}";

            // Si el POI tiene un objeto, el personaje se lo queda
            Item botin = poiActual.EntregarObjeto();
            if (botin != null)
            {
                if (pjActual.EquiparItem(botin))
                {
                    registro += $"\n<color=yellow>¡Has obtenido: {botin.Nombre}!</color>";
                }
                else
                {
                    registro += $"\n<color=red>Has encontrado {botin.Nombre}, pero no tienes espacio en el inventario.</color>";
                }
            }
        }
        else
        {
            registro += $"<color=red>¡FALLO!</color> No logras forzar el mecanismo. Has perdido el intento.";
        }

        pantallaUI.MostrarRegistroCombate(registro);
        pantallaUI.MostrarDatos(pjActual);
    }

    // ==========================================
    // INTELIGENCIA ARTIFICIAL (FASE DE EVENTOS)
    // ==========================================

    private void EjecutarTurnoEnemigos()
    {
        Personaje pjActual = listaPersonajes[indiceActual];
        string registroFase = "--- FASE DE EVENTOS ---\n";
        bool hayMonstruosVivos = false;

        // Iteramos sobre TODO el enjambre
        foreach (Enemigo monstruo in listaEnemigos)
        {
            if (monstruo.EstaMuerto) continue;
            hayMonstruosVivos = true;

            if (!pjActual.EstaMuerto)
            {
                if (pjActual.NodoActual == monstruo.NodoActual)
                {
                    // ESTADO 1: ATACAR
                    pjActual.RecibirDano(monstruo.DanoAtaque);
                    registroFase += $"El {monstruo.Nombre} te hace {monstruo.DanoAtaque} de daño.\n";
                    if (pjActual.EstaMuerto) registroFase += $"<color=red>¡{pjActual.Nombre} HA MUERTO!</color>\n";
                }
                else if (monstruo.NodoActual.Adyacentes.Contains(pjActual.NodoActual))
                {
                    // ESTADO 2: PERSEGUIR
                    monstruo.CambiarDeNodo(pjActual.NodoActual);
                    registroFase += $"¡El {monstruo.Nombre} ha entrado en la sala!\n";
                }
                else
                {
                    // ESTADO 3: PERDIDO
                    registroFase += $"Escuchas al {monstruo.Nombre} a lo lejos...\n";
                }
            }
        }

        if (!hayMonstruosVivos)
        {
            registroFase += "Todo está en silencio. No queda nadie vivo.\n";
        }

        pantallaUI.MostrarRegistroCombate(registroFase);
        pantallaUI.MostrarDatos(pjActual);

        Invoke("ReiniciarRonda", 2f);
    }
}
