using System;
using System.Collections.Generic;

// El atributo [Serializable] es OBLIGATORIO. Le dice al motor interno de Unity 
// que tiene permiso para "despedazar" esta clase y rellenarla con texto de un JSON.

// 1. El molde más pequeño (las llaves más internas del JSON)
[Serializable]
public class AtributoDTO
{
    public string NombreAtributo;
    public int CarasDado;
}

// 2. El molde del personaje (las llaves intermedias)
[Serializable]
public class PlantillaPersonajeDTO
{
    public string NombrePlantilla;
    public int VidaBase;

    // Aquí está el corchete de tu JSON: una Lista de Atributos
    public List<AtributoDTO> AtributosIniciales;
}

// 3. El contenedor raíz (las llaves que envuelven todo el archivo)
[Serializable]
public class ContenedorPlantillasDTO
{
    // El corchete principal de tu JSON
    public List<PlantillaPersonajeDTO> Plantillas;
}

// --- DTOs PARA ITEMS ---
[System.Serializable]
public class ItemDTO
{
    public string ID;
    public string Nombre;
    public int Categoria;
    public int SlotRequerido;
    public string AtributoUso;
    public int ValorEfecto;
}

[System.Serializable]
public class CatalogoItemsDTO
{
    public List<ItemDTO> Items;
}

// --- DTOs PARA MONSTRUOS ---
[System.Serializable]
public class EnemigoDTO
{
    public string ID;
    public string Nombre;
    public int VidaBase;
    public int DefensaDC;
    public int DanoAtaque;
    // NUEVO: Datos para leer desde el JSON
    public int RangoAudicion;
    public int TipoReposo; // 0 para Acechar, 1 para Deambular
}

[System.Serializable]
public class CatalogoEnemigosDTO
{
    public List<EnemigoDTO> Monstruos;
}

// ==========================================
// DTOs PARA PUNTOS DE INTERÉS NARRATIVOS (Árboles de decisión)
// ==========================================

[System.Serializable]
public class EventoNarrativoDTO
{
    public string ID;
    public string Titulo;
    public string IDPaginaInicial;
    public List<PaginaNarrativaDTO> Paginas;
}

[System.Serializable]
public class PaginaNarrativaDTO
{
    public string IDPagina;
    public string TextoDescriptivo;
    public List<OpcionNarrativaDTO> Opciones;
}

[System.Serializable]
public class OpcionNarrativaDTO
{
    public string TextoBoton;

    // 1. CONDICIONES (Opcionales)
    public string RequiereAtributo; // Ej: "Arcano"
    public int NivelDadoRequerido;  // Ej: 8 (Significa que necesita d8, d10 o d12)
    public string RequiereItem;     // Ej: "KEY_PLANOS"

    // 2. TIRADA (Opcional - Para opciones Híbridas)
    public string AtributoTirada;
    public int DificultadDC;

    // 3. CONSECUENCIAS (Outputs)
    public int ModificadorVida;      // Ej: -3 (Hace daño) o +5 (Cura)
    public int ModificadorVidaMaxima; // Ej: -1 (Trauma permanente)
    public int GeneraRuido;          // Ej: 4 (Atrae monstruos)
    public string EntregaItem;       // Ej: "WPN_REVOLVER"

    // 4. NAVEGACIÓN
    public string IDPaginaDestino;   // ID de la siguiente página. Si está vacío (""), el evento termina.
    public string TextoResultado;    // Texto que se muestra tras pulsar/tirar.
}

[System.Serializable]
public class CatalogoEventosDTO
{
    public List<EventoNarrativoDTO> Eventos;
}

// ==========================================
// DTOs PARA EL MAPA Y PUNTOS DE INTERÉS
// ==========================================
[System.Serializable]
public class POI_DTO
{
    public string Nombre;
    public string Descripcion;
    public bool EsNarrativo;

    // Datos para POI Mecánico
    public string AtributoRequerido;
    public int DificultadDC;
    public string RecompensaTexto;
    public string ItemRecompensaID;

    // Datos para POI Narrativo
    public string IDEventoNarrativo;
}

[System.Serializable]
public class NodoDTO
{
    public string ID;
    public string Nombre;
    public List<int> Estados; // 0=Oscuro, 1=Inundado, 2=Ardiendo, 3=Toxico, 4=Derrumbado
    public List<string> Conexiones; // IDs de las salas adyacentes
    public List<POI_DTO> PuntosInteres;
}

[System.Serializable]
public class MapaDTO
{
    public string NodoInicialID; // Donde aparecen los jugadores
    public List<NodoDTO> Nodos;
}