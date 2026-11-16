using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum FaseJuego
{
    Mantenimiento,
    Jugadores,
    Eventos,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Conexiones UI")]
    public InterfazJuego pantallaUI;

    // --- MEMORIA Y ESTADO ---
    private List<Personaje> listaPersonajes = new List<Personaje>();
    private int indiceActual = 0;
    private List<Enemigo> listaEnemigos = new List<Enemigo>();
    private List<Nodo> mapaNodos = new List<Nodo>();
    public FaseJuego faseActual;

    // --- BASES DE DATOS EN MEMORIA (JSON) ---
    private Dictionary<string, ItemDTO> catalogoItems = new Dictionary<string, ItemDTO>();
    private Dictionary<string, EnemigoDTO> catalogoEnemigos = new Dictionary<string, EnemigoDTO>();
    private Dictionary<string, EventoNarrativoDTO> catalogoEventos = new Dictionary<string, EventoNarrativoDTO>();

    // --- VARIABLES DEL SISTEMA NARRATIVO ---
    private EventoNarrativoDTO eventoActivo = null;
    private PaginaNarrativaDTO paginaActiva = null;

    private Dictionary<string, Nodo> diccionarioNodos = new Dictionary<string, Nodo>(); // Para buscar nodos rápido por ID
    void Start()
    {
        CargarCatalogos();

        // 1. CREACIÓN DEL MAPA DESDE JSON
        Nodo nodoSpawn = CargarMapaDesdeJSON();
        if (nodoSpawn == null)
        {
            Debug.LogError("Error fatal: No se pudo cargar el mapa o el nodo inicial.");
            return;
        }

        // 2. LECTURA Y CREACIÓN DE PERSONAJES
        string rutaArchivo = Path.Combine(Application.dataPath, "_Data", "plantillas_personajes.json");
        if (File.Exists(rutaArchivo))
        {
            string jsonCrudo = File.ReadAllText(rutaArchivo);
            ContenedorPlantillasDTO contenedor = JsonUtility.FromJson<ContenedorPlantillasDTO>(jsonCrudo);

            foreach (PlantillaPersonajeDTO plantillaDTO in contenedor.Plantillas)
            {
                Personaje nuevoPersonaje = new Personaje(plantillaDTO.NombrePlantilla, plantillaDTO.VidaBase);
                foreach (AtributoDTO atributoDTO in plantillaDTO.AtributosIniciales)
                {
                    nuevoPersonaje.AsignarAtributo(atributoDTO.NombreAtributo, (TipoDado)atributoDTO.CarasDado);
                }

                Item armaInicial = CrearItemDesdeCatalogo("WPN_REVOLVER");
                if (armaInicial != null) nuevoPersonaje.EquiparItem(armaInicial);

                nuevoPersonaje.EstablecerUbicacionInicial(nodoSpawn); // Aparecen en el NodoInicialID del JSON
                listaPersonajes.Add(nuevoPersonaje);
            }
        }

        if (listaPersonajes.Count > 0) pantallaUI.MostrarDatos(listaPersonajes[0]);

        // 3. GENERACIÓN DE ENEMIGOS
        if (catalogoEnemigos.ContainsKey("MNST_HIBRIDO"))
        {
            EnemigoDTO datos = catalogoEnemigos["MNST_HIBRIDO"];

            Enemigo monstruoA = new Enemigo($"{datos.Nombre} A", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque, datos.RangoAudicion, (ComportamientoReposo)datos.TipoReposo);
            monstruoA.EstablecerUbicacionInicial(diccionarioNodos["NOD_PASILLO"]);
            listaEnemigos.Add(monstruoA);

            Enemigo monstruoB = new Enemigo($"{datos.Nombre} B", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque, datos.RangoAudicion, (ComportamientoReposo)datos.TipoReposo);
            monstruoB.EstablecerUbicacionInicial(diccionarioNodos["NOD_BIBLIO"]);
            listaEnemigos.Add(monstruoB);
        }

        CambiarFase(FaseJuego.Mantenimiento);
    }

    private Nodo CargarMapaDesdeJSON()
    {
        string rutaMapa = Path.Combine(Application.dataPath, "_Data", "mapa.json");
        if (!File.Exists(rutaMapa)) return null;

        MapaDTO mapaDTO = JsonUtility.FromJson<MapaDTO>(File.ReadAllText(rutaMapa));
        diccionarioNodos.Clear();
        mapaNodos.Clear();

        // PRIMERA PASADA: Instanciar Nodos, Peligros y POIs
        foreach (NodoDTO nDTO in mapaDTO.Nodos)
        {
            Nodo nuevoNodo = new Nodo(nDTO.Nombre);

            foreach (int estado in nDTO.Estados) nuevoNodo.AnadirEstado((EstadoEntorno)estado);

            foreach (POI_DTO poiDTO in nDTO.PuntosInteres)
            {
                PuntoInteres nuevoPOI;
                if (poiDTO.EsNarrativo)
                {
                    nuevoPOI = new PuntoInteres(poiDTO.Nombre, poiDTO.Descripcion, poiDTO.IDEventoNarrativo);
                }
                else
                {
                    Item recompensa = string.IsNullOrEmpty(poiDTO.ItemRecompensaID) ? null : CrearItemDesdeCatalogo(poiDTO.ItemRecompensaID);
                    nuevoPOI = new PuntoInteres(poiDTO.Nombre, poiDTO.Descripcion, poiDTO.AtributoRequerido, poiDTO.DificultadDC, poiDTO.RecompensaTexto, recompensa);
                }
                nuevoNodo.AgregarPOI(nuevoPOI);
            }

            diccionarioNodos.Add(nDTO.ID, nuevoNodo);
            mapaNodos.Add(nuevoNodo);
        }

        // SEGUNDA PASADA: Conectar los nodos usando sus IDs
        foreach (NodoDTO nDTO in mapaDTO.Nodos)
        {
            Nodo nodoActual = diccionarioNodos[nDTO.ID];
            foreach (string idConexion in nDTO.Conexiones)
            {
                if (diccionarioNodos.ContainsKey(idConexion))
                {
                    nodoActual.ConectarCon(diccionarioNodos[idConexion]);
                }
            }
        }

        return diccionarioNodos.ContainsKey(mapaDTO.NodoInicialID) ? diccionarioNodos[mapaDTO.NodoInicialID] : null;
    }

    private void CargarCatalogos()
    {
        string rutaItems = Path.Combine(Application.dataPath, "_Data", "items.json");
        if (File.Exists(rutaItems))
        {
            CatalogoItemsDTO catItems = JsonUtility.FromJson<CatalogoItemsDTO>(File.ReadAllText(rutaItems));
            foreach (ItemDTO i in catItems.Items) { catalogoItems[i.ID] = i; }
        }

        string rutaMonstruos = Path.Combine(Application.dataPath, "_Data", "monstruos.json");
        if (File.Exists(rutaMonstruos))
        {
            CatalogoEnemigosDTO catMonstruos = JsonUtility.FromJson<CatalogoEnemigosDTO>(File.ReadAllText(rutaMonstruos));
            foreach (EnemigoDTO e in catMonstruos.Monstruos) { catalogoEnemigos[e.ID] = e; }
        }

        string rutaEventos = Path.Combine(Application.dataPath, "_Data", "eventos.json");
        if (File.Exists(rutaEventos))
        {
            CatalogoEventosDTO catEventos = JsonUtility.FromJson<CatalogoEventosDTO>(File.ReadAllText(rutaEventos));
            foreach (EventoNarrativoDTO ev in catEventos.Eventos) { catalogoEventos[ev.ID] = ev; }
        }
    }

    public Item CrearItemDesdeCatalogo(string id)
    {
        if (catalogoItems.ContainsKey(id))
        {
            ItemDTO dto = catalogoItems[id];
            return new Item(dto.Nombre, (CategoriaItem)dto.Categoria, (SlotEquipamiento)dto.SlotRequerido, dto.AtributoUso, dto.ValorEfecto);
        }
        return null;
    }

    // ==========================================
    // CEREBRO DEL JUEGO (CONTROL DE FASES)
    // ==========================================

    private bool ComprobarGameOver()
    {
        // Comprueba si queda al menos un investigador con vida
        foreach (Personaje pj in listaPersonajes)
        {
            if (!pj.EstaMuerto) return false;
        }
        return true; // Todos han muerto
    }
    public void CambiarFase(FaseJuego nuevaFase)
    {
        faseActual = nuevaFase;
        switch (faseActual)
        {
            case FaseJuego.Mantenimiento:
                string registroMantenimiento = "--- FASE DE MANTENIMIENTO ---\nRestaurando acciones y disipando ruido...\n";

                // 1. El entorno se asienta
                foreach (Nodo n in mapaNodos) n.DisiparRuido();

                // 2. Restauración y Peligros Ambientales
                foreach (Personaje pj in listaPersonajes)
                {
                    if (pj.EstaMuerto) continue;

                    pj.RestaurarAcciones();

                    // DAÑO POR ENTORNO: Si terminaste el turno en una sala peligrosa, pagas el precio
                    if (pj.NodoActual.TieneEstado(EstadoEntorno.Ardiendo))
                    {
                        pj.RecibirDano(1);
                        registroMantenimiento += $"<color=orange>¡{pj.Nombre} se quema entre las llamas!</color> (-1 PV)\n";
                    }
                    if (pj.NodoActual.TieneEstado(EstadoEntorno.Toxico))
                    {
                        pj.RecibirDano(1);
                        registroMantenimiento += $"<color=green>¡{pj.Nombre} respira gas tóxico!</color> (-1 PV)\n";
                    }
                    if (pj.NodoActual.TieneEstado(EstadoEntorno.Inundado))
                    {
                        // Podríamos restar 1 Acción en lugar de vida si está inundado, etc.
                    }
                }

                pantallaUI.MostrarRegistroCombate(registroMantenimiento);
                pantallaUI.MostrarDatos(listaPersonajes[indiceActual]);

                // 3. Chequeo Crítico: ¿Sigue alguien con vida?
                if (ComprobarGameOver())
                {
                    CambiarFase(FaseJuego.GameOver);
                }
                else
                {
                    Invoke("PasarAJugadores", 2.0f); // Damos 2 segundos para leer el daño antes de jugar
                }
                break;

            case FaseJuego.Jugadores:
                pantallaUI.MostrarRegistroCombate("--- FASE DE JUGADORES ---\nEs tu turno. Elige tus acciones.");
                break;

            case FaseJuego.Eventos:
                pantallaUI.MostrarRegistroCombate("--- FASE DE EVENTOS ---\nLos monstruos actúan...");
                EjecutarTurnoEnemigos();
                break;

            case FaseJuego.GameOver: // NUEVO
                pantallaUI.MostrarRegistroCombate("<color=red>=== FIN DE LA PARTIDA ===</color>\nTodos los investigadores han muerto o sucumbido a la locura. La oscuridad prevalece.");
                break;
        }
    }

    private void PasarAJugadores() { CambiarFase(FaseJuego.Jugadores); }
    private void ReiniciarRonda() { CambiarFase(FaseJuego.Mantenimiento); }

    // ==========================================
    // CONTROLES DE LA INTERFAZ (TÁCTICOS)
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
        if (faseActual == FaseJuego.Jugadores && eventoActivo == null) CambiarFase(FaseJuego.Eventos);
    }

    public void TirarDadoPrueba()
    {
        if (faseActual != FaseJuego.Jugadores || eventoActivo != null) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        Enemigo objetivo = null;
        foreach (Enemigo e in listaEnemigos)
        {
            if (!e.EstaMuerto && e.NodoActual == pjActual.NodoActual)
            {
                objetivo = e;
                break;
            }
        }

        if (objetivo == null)
        {
            pantallaUI.MostrarRegistroCombate("No hay monstruos aquí.");
            return;
        }
        if (!pjActual.GastarAccion()) return;

        int resultadoTirada = pjActual.RealizarTirada("Fuerza");
        string registro = $"{pjActual.Nombre} ataca al {objetivo.Nombre}...\n";

        if (resultadoTirada >= objetivo.DefensaDC)
        {
            objetivo.RecibirDano(1);
            registro += $"<color=green>¡ÉXITO!</color> El enemigo recibe 1 de daño.\n";
        }
        else
        {
            registro += "<color=red>¡FALLO!</color> El ataque no surte efecto.";
        }

        pantallaUI.MostrarRegistroCombate(registro);
        pantallaUI.MostrarDatos(pjActual);
    }

    public void BotonMoverPrueba()
    {
        if (faseActual != FaseJuego.Jugadores || eventoActivo != null) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto || pjActual.NodoActual.Adyacentes.Count == 0 || !pjActual.GastarAccion()) return;

        Nodo destino = pjActual.NodoActual.Adyacentes[0];
        pjActual.CambiarDeNodo(destino);
        destino.GenerarRuido(1);

        pantallaUI.MostrarRegistroCombate($"{pjActual.Nombre} camina hacia: {destino.Nombre} (Genera 1 Ruido).");
        pantallaUI.MostrarDatos(pjActual);
    }

    public void BotonInteractuarPrueba()
    {
        if (faseActual != FaseJuego.Jugadores || eventoActivo != null) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        // Buscamos si hay algo con lo que interactuar en esta sala
        PuntoInteres poiActual = null;
        foreach (var p in pjActual.NodoActual.PuntosDeInteres)
        {
            if (!p.YaInteractuado) { poiActual = p; break; }
        }

        if (poiActual == null)
        {
            pantallaUI.MostrarRegistroCombate("No hay nada de interés aquí.");
            return;
        }

        // BIFURCACIÓN: ¿Es un evento de historia o una prueba mecánica rápida?
        if (poiActual.EsNarrativo)
        {
            poiActual.MarcarComoInteractuado();
            IniciarEventoNarrativo(poiActual.IDEventoNarrativo);
        }
        else
        {
            if (!pjActual.GastarAccion()) return;

            int resultadoTirada = pjActual.RealizarTirada(poiActual.AtributoRequerido);
            string registro = $"{pjActual.Nombre} investiga '{poiActual.Nombre}'...\n";

            if (resultadoTirada >= poiActual.DificultadDC)
            {
                poiActual.MarcarComoInteractuado();
                registro += $"<color=green>¡ÉXITO!</color> {poiActual.RecompensaTexto}";
                Item botin = poiActual.EntregarObjeto();
                if (botin != null) pjActual.EquiparItem(botin);
            }
            else
            {
                pjActual.NodoActual.GenerarRuido(3);
                registro += $"<color=red>¡FALLO!</color> Provocas un gran estruendo al intentarlo (Genera 3 Ruido).";
            }
            pantallaUI.MostrarRegistroCombate(registro);
            pantallaUI.MostrarDatos(pjActual);
        }
    }

        // ==========================================
        // SISTEMA NARRATIVO Y TECLADO
        // ==========================================
        void Update()
        {
            // Solo escuchamos el teclado si hay un evento en curso
            if (eventoActivo != null && paginaActiva != null)
            {
                // Opción 1 (Teclas 1 superior o 1 Numpad)
                if ((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) && paginaActiva.Opciones.Count >= 1)
                    ProcesarOpcionNarrativa(paginaActiva.Opciones[0], eventoActivo.ID);

                // Opción 2 (Teclas 2 superior o 2 Numpad)
                if ((Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) && paginaActiva.Opciones.Count >= 2)
                    ProcesarOpcionNarrativa(paginaActiva.Opciones[1], eventoActivo.ID);

                // Opción 3 (Teclas 3 superior o 3 Numpad)
                if ((Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) && paginaActiva.Opciones.Count >= 3)
                    ProcesarOpcionNarrativa(paginaActiva.Opciones[2], eventoActivo.ID);
            }
        }
    

    public void IniciarEventoNarrativo(string idEvento)
    {
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        if (!catalogoEventos.ContainsKey(idEvento)) return;

        EventoNarrativoDTO evento = catalogoEventos[idEvento];
        PaginaNarrativaDTO paginaInicial = evento.Paginas.Find(p => p.IDPagina == evento.IDPaginaInicial);

        if (paginaInicial != null)
        {
            eventoActivo = evento;
            paginaActiva = paginaInicial;
            pantallaUI.MostrarPaginaNarrativa(pjActual, evento.Titulo, paginaInicial, idEvento);
        }
    }

    public void ProcesarOpcionNarrativa(OpcionNarrativaDTO opcionElegida, string idEventoActual)
    {
        Personaje pjActual = listaPersonajes[indiceActual];
        string registro = "";

        if (!string.IsNullOrEmpty(opcionElegida.RequiereAtributo))
        {
            if (!pjActual.Atributos.ContainsKey(opcionElegida.RequiereAtributo) || (int)pjActual.Atributos[opcionElegida.RequiereAtributo] < opcionElegida.NivelDadoRequerido)
            {
                pantallaUI.MostrarRegistroCombate($"<color=grey>Opción bloqueada. Necesitas d{opcionElegida.NivelDadoRequerido} en {opcionElegida.RequiereAtributo}.</color>");
                return;
            }
        }

        bool exito = true;
        if (!string.IsNullOrEmpty(opcionElegida.AtributoTirada) && opcionElegida.DificultadDC > 0)
        {
            if (!pjActual.GastarAccion())
            {
                pantallaUI.MostrarRegistroCombate("<color=red>¡Sin energía!</color> No puedes realizar esta acción.");
                return;
            }

            int resultado = pjActual.RealizarTirada(opcionElegida.AtributoTirada);
            registro += $"Tirada de {opcionElegida.AtributoTirada}: {resultado} (DC {opcionElegida.DificultadDC})\n";

            if (resultado < opcionElegida.DificultadDC)
            {
                exito = false;
                registro += $"<color=red>¡FALLO!</color> No consigues tu objetivo.\n";
            }
            else
            {
                registro += $"<color=green>¡ÉXITO!</color>\n";
            }
        }

        if (exito)
        {
            registro += opcionElegida.TextoResultado + "\n";

            if (opcionElegida.ModificadorVida < 0) pjActual.RecibirDano(Mathf.Abs(opcionElegida.ModificadorVida));
            if (opcionElegida.GeneraRuido > 0) pjActual.NodoActual.GenerarRuido(opcionElegida.GeneraRuido);

            if (!string.IsNullOrEmpty(opcionElegida.EntregaItem))
            {
                Item botin = CrearItemDesdeCatalogo(opcionElegida.EntregaItem);
                if (botin != null) pjActual.EquiparItem(botin);
            }

            if (string.IsNullOrEmpty(opcionElegida.IDPaginaDestino))
            {
                eventoActivo = null;
                paginaActiva = null;
                registro += "\n<color=yellow>--- FIN DEL EVENTO ---</color>";
                pantallaUI.MostrarRegistroCombate(registro);
                pantallaUI.MostrarDatos(pjActual);
            }
            else
            {
                // Aquí nacen las variables
                EventoNarrativoDTO eventoActual = catalogoEventos[idEventoActual];
                PaginaNarrativaDTO siguientePagina = eventoActual.Paginas.Find(p => p.IDPagina == opcionElegida.IDPaginaDestino);
                paginaActiva = siguientePagina;

                pantallaUI.MostrarRegistroCombate(registro);

                // Y aquí las usamos junto con idEventoActual
                pantallaUI.MostrarPaginaNarrativa(pjActual, eventoActual.Titulo, siguientePagina, idEventoActual);
            }
        }
        else
        {
            pantallaUI.MostrarRegistroCombate(registro);
            pantallaUI.MostrarPaginaNarrativa(pjActual, eventoActivo.Titulo, paginaActiva, idEventoActual);
        }
    }

    // ==========================================
    // INTELIGENCIA ARTIFICIAL (RADARES Y PATHFINDING BFS)
    // ==========================================
    private Nodo CalcularSiguientePasoHaciaRuido(Nodo inicio, int rangoMaximo)
    {
        Queue<Nodo> cola = new Queue<Nodo>();
        Dictionary<Nodo, int> distancias = new Dictionary<Nodo, int>();
        Dictionary<Nodo, Nodo> padres = new Dictionary<Nodo, Nodo>();
        List<Nodo> nodosConRuido = new List<Nodo>();

        cola.Enqueue(inicio);
        distancias[inicio] = 0;
        padres[inicio] = null;

        while (cola.Count > 0)
        {
            Nodo actual = cola.Dequeue();
            int dist = distancias[actual];

            if (actual.NivelRuido > 0) nodosConRuido.Add(actual);

            if (dist < rangoMaximo)
            {
                foreach (Nodo vecino in actual.Adyacentes)
                {
                    if (!distancias.ContainsKey(vecino))
                    {
                        distancias[vecino] = dist + 1;
                        padres[vecino] = actual;
                        cola.Enqueue(vecino);
                    }
                }
            }
        }

        if (nodosConRuido.Count == 0) return null;

        Nodo objetivo = null;
        int maxRuido = -1;
        int minDistancia = int.MaxValue;

        foreach (Nodo n in nodosConRuido)
        {
            if (n.NivelRuido > maxRuido || (n.NivelRuido == maxRuido && distancias[n] < minDistancia))
            {
                maxRuido = n.NivelRuido;
                minDistancia = distancias[n];
                objetivo = n;
            }
        }

        if (objetivo == null || objetivo == inicio) return null;

        Nodo paso = objetivo;
        while (padres[paso] != inicio) paso = padres[paso];

        return paso;
    }

    private void EjecutarTurnoEnemigos()
    {
        Personaje pjActual = listaPersonajes[indiceActual];
        string registroFase = "--- FASE DE EVENTOS ---\n";
        bool hayMonstruosVivos = false;

        foreach (Enemigo monstruo in listaEnemigos)
        {
            if (monstruo.EstaMuerto) continue;
            hayMonstruosVivos = true;

            if (pjActual.EstaMuerto) continue;

            if (pjActual.NodoActual == monstruo.NodoActual)
            {
                pjActual.RecibirDano(monstruo.DanoAtaque);
                registroFase += $"El {monstruo.Nombre} ataca: -{monstruo.DanoAtaque} PV.\n";
            }
            else if (monstruo.NodoActual.Adyacentes.Contains(pjActual.NodoActual))
            {
                monstruo.CambiarDeNodo(pjActual.NodoActual);
                registroFase += $"¡El {monstruo.Nombre} ha entrado en la sala!\n";
            }
            else
            {
                Nodo rastro = CalcularSiguientePasoHaciaRuido(monstruo.NodoActual, monstruo.RangoAudicion);

                if (rastro != null)
                {
                    monstruo.CambiarDeNodo(rastro);
                    registroFase += $"Escuchas pasos... El {monstruo.Nombre} se acerca hacia el ruido.\n";
                }
                else
                {
                    if (monstruo.Reposo == ComportamientoReposo.Deambular && monstruo.NodoActual.Adyacentes.Count > 0)
                    {
                        int rnd = Random.Range(0, monstruo.NodoActual.Adyacentes.Count);
                        monstruo.CambiarDeNodo(monstruo.NodoActual.Adyacentes[rnd]);
                        registroFase += $"El {monstruo.Nombre} deambula por el mapa...\n";
                    }
                    else
                    {
                        registroFase += $"El {monstruo.Nombre} acecha en silencio.\n";
                    }
                }
            }
        }

        if (!hayMonstruosVivos) registroFase += "Todo está en silencio.\n";

        pantallaUI.MostrarRegistroCombate(registroFase);
        pantallaUI.MostrarDatos(pjActual);
        Invoke("ReiniciarRonda", 2f);
    }
}