using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InterfazJuego : MonoBehaviour
{
    public TextMeshProUGUI textoPantalla;
    public TextMeshProUGUI TextoResultado;

   public void MostrarDatos(Personaje pj)
    {
        string textoFinal = $"FICHA: {pj.Nombre}\n";
        
        if (pj.NodoActual != null)
        {
            textoFinal += $"UBICACIÓN: {pj.NodoActual.Nombre}\n";
        }
        
        textoFinal += $"VIDA: {pj.VidaActual} / {pj.VidaMaxima}\n"; 
        textoFinal += $"ACCIONES: {pj.AccionesActuales} / {pj.AccionesMaximas}\n"; 
        
        if (pj.Inventario.ContainsKey(SlotEquipamiento.ManoDerecha))
        {
            Item armaEquipada = pj.Inventario[SlotEquipamiento.ManoDerecha];
            textoFinal += $"MANO DCHA: {armaEquipada.Nombre} (Daño: {armaEquipada.ValorEfecto})\n";
        }
        else
        {
            textoFinal += "MANO DCHA: Vacía (Puños)\n";
        }

        // --- NUEVO: MOSTRAR EQUIPAMIENTO ADICIONAL (RELIQUIAS, CHALECOS, ETC.) ---
        if (pj.EquipamientoAdicional != null && pj.EquipamientoAdicional.Count > 0)
        {
            textoFinal += "EQUIPO Y RELIQUIAS:\n";
            foreach (Item equipo in pj.EquipamientoAdicional)
            {
                textoFinal += $"> {equipo.Nombre}\n";
            }
        }
        else
        {
            textoFinal += "EQUIPO: Ninguno\n";
        }
        // -------------------------------------------------------------------------

        textoFinal += "--------------------\n";
        
        // --- AQUÍ ESTÁ EL BLOQUE QUE FALTABA PARA MOSTRAR LOS POIs ---
        if (pj.NodoActual != null && pj.NodoActual.PuntosDeInteres != null && pj.NodoActual.PuntosDeInteres.Count > 0)
        {
            textoFinal += "PUNTOS DE INTERÉS:\n";
            foreach (PuntoInteres poi in pj.NodoActual.PuntosDeInteres)
            {
                string estado = poi.YaInteractuado ? "<color=gray>(Explorado)</color>" : "<color=green>(Disponible)</color>";
                textoFinal += $"> {poi.Nombre} {estado}\n";
                textoFinal += $"  [Requiere: {poi.AtributoRequerido} | DC: {poi.DificultadDC}]\n";
            }
            textoFinal += "--------------------\n";
        }
        // -------------------------------------------------------------

        textoFinal += "ATRIBUTOS:\n";

        foreach (KeyValuePair<string, TipoDado> atributo in pj.Atributos) 
        {
            textoFinal += $"> {atributo.Key}: {atributo.Value}\n";
        }

        textoPantalla.text = textoFinal;
    }



    public void MostrarRegistroCombate(string mensaje)
    {
        TextoResultado.text = mensaje;
    }
}