using System.Collections.Generic;

// 1. Tipos de cosas que pueden pasar al elegir una opción
public enum TipoConsecuencia
{
    Ninguna,
    DarItem,
    Curar,
    HacerDano,
    GenerarRuido,
    AnadirEstadoEntorno
}

// 2. La capsula que guarda los datos de esa consecuencia
public class Consecuencia
{
    public TipoConsecuencia Tipo { get; private set; }
    public string ParametroTexto { get; private set; } // Ej: ID de un Item ("WPN_REVOLVER") o un Estado ("Ardiendo")
    public int ParametroNumerico { get; private set; } // Ej: 3 de daño, 2 de ruido, etc.

    public Consecuencia(TipoConsecuencia tipo, string paramTexto, int paramNum)
    {
        Tipo = tipo;
        ParametroTexto = paramTexto;
        ParametroNumerico = paramNum;
    }
}

// 3. Un botón/opción que el jugador puede pulsar en un evento narrativo
public class OpcionNarrativa
{
    public string TextoBoton { get; private set; }
    public int SiguienteNodoID { get; private set; } // 0 significa que al pulsar esto, el evento se acaba y se cierra

    // CONDICIONES (Para que el botón se muestre o habilite)
    public string ItemRequeridoID { get; private set; }

    // HIBRIDACIÓN MECÁNICA (Si pulsar el botón requiere tirar un dado para ver qué pasa)
    public bool RequiereTirada { get; private set; }
    public string AtributoTirada { get; private set; }
    public int DificultadDC { get; private set; }

    // CONSECUENCIAS
    public List<Consecuencia> ConsecuenciasExito { get; private set; }
    public List<Consecuencia> ConsecuenciasFallo { get; private set; } // Solo aplican si había tirada y se falló

    public OpcionNarrativa(string texto, int siguienteNodo)
    {
        TextoBoton = texto;
        SiguienteNodoID = siguienteNodo;
        ConsecuenciasExito = new List<Consecuencia>();
        ConsecuenciasFallo = new List<Consecuencia>();
    }

    public void ConfigurarCondicion(string itemID)
    {
        ItemRequeridoID = itemID;
    }

    public void ConfigurarTirada(string atributo, int dc)
    {
        RequiereTirada = true;
        AtributoTirada = atributo;
        DificultadDC = dc;
    }
}

// 4. Una "página" dentro del árbol del evento (Ej: "Abres el sarcófago y ves oro. ¿Qué haces?")
public class NodoNarrativo
{
    public int ID { get; private set; }
    public string TextoDescriptivo { get; private set; }
    public List<OpcionNarrativa> Opciones { get; private set; }

    public NodoNarrativo(int id, string texto)
    {
        ID = id;
        TextoDescriptivo = texto;
        Opciones = new List<OpcionNarrativa>();
    }
}

// 5. El Contenedor Principal (Reemplaza a tu antigua clase)
public class PuntoInteres
{
    public string Nombre { get; private set; }
    public string Descripcion { get; private set; }
    public bool YaInteractuado { get; private set; }

    // ==========================================
    // MODO 1: MECÁNICO CLÁSICO (Tirada directa)
    // ==========================================
    public string AtributoRequerido { get; private set; }
    public int DificultadDC { get; private set; }
    public string RecompensaTexto { get; private set; }
    private Item itemRecompensa;

    // ==========================================
    // MODO 2: NARRATIVO (Librojuego JSON)
    // ==========================================
    public string IDEventoNarrativo { get; private set; }
    public bool EsNarrativo { get; private set; }

    // Constructor 1: Mecánico (Usado actualmente para la Caja Fuerte)
    public PuntoInteres(string nombre, string descripcion, string atributoRequerido, int dificultadDC, string recompensaTexto, Item recompensa)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        AtributoRequerido = atributoRequerido;
        DificultadDC = dificultadDC;
        RecompensaTexto = recompensaTexto;
        itemRecompensa = recompensa;

        YaInteractuado = false;
        EsNarrativo = false; // El motor sabe que es un POI de 1 solo clic
    }

    // Constructor 2: Narrativo (Usado para disparar eventos como El Altar Impío)
    public PuntoInteres(string nombre, string descripcion, string idEventoNarrativo)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        IDEventoNarrativo = idEventoNarrativo;

        YaInteractuado = false;
        EsNarrativo = true; // El motor sabe que debe cargar el JSON
    }

    public void MarcarComoInteractuado()
    {
        YaInteractuado = true;
    }

    public Item EntregarObjeto()
    {
        return itemRecompensa;
    }
}