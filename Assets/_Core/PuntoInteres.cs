using System;
using UnityEngine;

public class PuntoInteres
{
    public string Nombre { get; private set; }
    public string Descripcion { get; private set; }
    public string AtributoRequerido { get; private set; }
    public int DificultadDC { get; private set; }
    public bool YaInteractuado { get; private set; }

    // Lo que otorga al tener éxito (ej. un objeto o un evento)
    public string RecompensaTexto { get; private set; }
    public Item ObjetoRecompensa { get; private set; }

    public PuntoInteres(string nombre, string descripcion, string atributo, int dc, string recompensa, Item objeto = null)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        AtributoRequerido = atributo;
        DificultadDC = dc;
        RecompensaTexto = recompensa;
        YaInteractuado = false;
        ObjetoRecompensa = objeto;
    }

    public void MarcarComoInteractuado()
    {
        YaInteractuado = true;
    }

    public Item EntregarObjeto()
    {
        Item objetoEntregado = ObjetoRecompensa;
        ObjetoRecompensa = null;
        return objetoEntregado;
    }
}