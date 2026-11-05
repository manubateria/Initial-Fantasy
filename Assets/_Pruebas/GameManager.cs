using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    private List<Enemigo> listaEnemigos = new List<Enemigo>();

    // NUEVO: Lista maestra del mapa para disipar el ruido global
    private List<Nodo> mapaNodos = new List<Nodo>();

    public FaseJuego faseActual;

    // --- BASES DE DATOS EN MEMORIA (JSON) ---
    private Dictionary<string, ItemDTO> catalogoItems = new Dictionary<string, ItemDTO>();
    private Dictionary<string, EnemigoDTO> catalogoEnemigos = new Dictionary<string, EnemigoDTO>();

    void Start()
    {
        CargarCatalogos();

        // 1. CREACIÓN DEL MAPA (GRAFO ESPACIAL)
        Nodo pasillo = new Nodo("Pasillo Oscuro");
        pasillo.AnadirEstado(EstadoEntorno.Oscuro); // <--- AÑADE ESTA LÍNEA
        Nodo biblioteca = new Nodo("Biblioteca Oculta");
        biblioteca.AnadirEstado(EstadoEntorno.Ardiendo); // <--- AÑADE ESTA LÍNEA
        biblioteca.AnadirEstado(EstadoEntorno.Inundado); // <--- Y ESTA (¡Fuego y agua!)
        pasillo.ConectarCon(biblioteca);

        mapaNodos.Add(pasillo);
        mapaNodos.Add(biblioteca);

        Item documentos = CrearItemDesdeCatalogo("KEY_PLANOS");
        PuntoInteres cajaFuerte = new PuntoInteres(
            "Caja Fuerte Oxidada",
            "Una caja fuerte empotrada.",
            "Destreza",
            5,
            "¡Has forzado la cerradura y obtenido documentos!",
            documentos
        );
        biblioteca.AgregarPOI(cajaFuerte);

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

                nuevoPersonaje.EstablecerUbicacionInicial(pasillo);
                listaPersonajes.Add(nuevoPersonaje);
            }
        }

        if (listaPersonajes.Count > 0)
        {
            pantallaUI.MostrarDatos(listaPersonajes[0]);
        }

        // 3. GENERACIÓN DE ENEMIGOS
        if (catalogoEnemigos.ContainsKey("MNST_HIBRIDO"))
        {
            EnemigoDTO datos = catalogoEnemigos["MNST_HIBRIDO"];

            // ACTUALIZADO: Le inyectamos la IA (Rango y Reposo) desde el JSON
            Enemigo monstruoA = new Enemigo($"{datos.Nombre} A", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque, datos.RangoAudicion, (ComportamientoReposo)datos.TipoReposo);
            monstruoA.EstablecerUbicacionInicial(pasillo);
            listaEnemigos.Add(monstruoA);

            Enemigo monstruoB = new Enemigo($"{datos.Nombre} B", datos.VidaBase, datos.DefensaDC, datos.DanoAtaque, datos.RangoAudicion, (ComportamientoReposo)datos.TipoReposo);
            monstruoB.EstablecerUbicacionInicial(biblioteca);
            listaEnemigos.Add(monstruoB);
        }

        CambiarFase(FaseJuego.Mantenimiento);
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
    public void CambiarFase(FaseJuego nuevaFase)
    {
        faseActual = nuevaFase;
        switch (faseActual)
        {
            case FaseJuego.Mantenimiento:
                pantallaUI.MostrarRegistroCombate("--- FASE DE MANTENIMIENTO ---\nRestaurando acciones y disipando ruido...");

                // NUEVO: La acústica se asienta en el mapa
                foreach (Nodo n in mapaNodos)
                {
                    n.DisiparRuido();
                }

                foreach (Personaje pj in listaPersonajes)
                {
                    pj.RestaurarAcciones();
                }
                pantallaUI.MostrarDatos(listaPersonajes[indiceActual]);
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

    private void PasarAJugadores() { CambiarFase(FaseJuego.Jugadores); }
    private void ReiniciarRonda() { CambiarFase(FaseJuego.Mantenimiento); }

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
        if (faseActual == FaseJuego.Jugadores) CambiarFase(FaseJuego.Eventos);
    }

    public void TirarDadoPrueba()
    {
        if (faseActual != FaseJuego.Jugadores) return;
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
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto || pjActual.NodoActual.Adyacentes.Count == 0 || !pjActual.GastarAccion()) return;

        Nodo destino = pjActual.NodoActual.Adyacentes[0];
        pjActual.CambiarDeNodo(destino);

        // NUEVO: Caminar apresuradamente genera un poco de ruido
        destino.GenerarRuido(1);

        pantallaUI.MostrarRegistroCombate($"{pjActual.Nombre} camina hacia: {destino.Nombre} (Genera 1 Ruido).");
        pantallaUI.MostrarDatos(pjActual);
    }

    public void BotonInteractuarPrueba()
    {
        if (faseActual != FaseJuego.Jugadores) return;
        Personaje pjActual = listaPersonajes[indiceActual];
        if (pjActual.EstaMuerto) return;

        PuntoInteres poiActual = null;
        foreach (var p in pjActual.NodoActual.PuntosDeInteres)
        {
            if (!p.YaInteractuado) { poiActual = p; break; }
        }

        if (poiActual == null || !pjActual.GastarAccion()) return;

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
            // NUEVO: Fallar una prueba de destreza o fuerza provoca mucho ruido
            pjActual.NodoActual.GenerarRuido(3);
            registro += $"<color=red>¡FALLO!</color> Provocas un gran estruendo al intentarlo (Genera 3 Ruido).";
        }
        pantallaUI.MostrarRegistroCombate(registro);
        pantallaUI.MostrarDatos(pjActual);
    }

    // ==========================================
    // INTELIGENCIA ARTIFICIAL (RADARES Y PATHFINDING)
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

        // Propagación de onda (Búsqueda en Anchura)
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

        if (nodosConRuido.Count == 0) return null; // Nadie hizo ruido

        // Criterio de prioridad: Máximo nivel de ruido. En caso de empate, el más cercano.
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

        // Reconstrucción del camino hacia atrás para saber cuál es el primer paso desde nuestro origen
        Nodo paso = objetivo;
        while (padres[paso] != inicio)
        {
            paso = padres[paso];
        }

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
                // ESTADO 1: ATACAR (Prioridad Máxima)
                pjActual.RecibirDano(monstruo.DanoAtaque);
                registroFase += $"El {monstruo.Nombre} ataca: -{monstruo.DanoAtaque} PV.\n";
            }
            else if (monstruo.NodoActual.Adyacentes.Contains(pjActual.NodoActual))
            {
                // ESTADO 2: PERSEGUIR (Visión directa en la habitación de al lado)
                monstruo.CambiarDeNodo(pjActual.NodoActual);
                registroFase += $"¡El {monstruo.Nombre} ha entrado en la sala!\n";
            }
            else
            {
                // ESTADO 3: RASTREAR RUIDO (Algoritmo BFS)
                Nodo rastro = CalcularSiguientePasoHaciaRuido(monstruo.NodoActual, monstruo.RangoAudicion);

                if (rastro != null)
                {
                    monstruo.CambiarDeNodo(rastro);
                    registroFase += $"Escuchas pasos... El {monstruo.Nombre} se acerca hacia el ruido.\n";
                }
                else
                {
                    // ESTADO 4: REPOSO
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