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
}

[System.Serializable]
public class CatalogoEnemigosDTO
{
    public List<EnemigoDTO> Monstruos;
}