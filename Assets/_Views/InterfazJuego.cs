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
        string estadoTexto = pj.EstaMuerto ? "<color=red>MUERTO</color>" : pj.EstadoPsicologico.ToString().ToUpper();
        textoFinal += $"ESTADO: {estadoTexto}\n";

        if (pj.NodoActual != null)
        {
            textoFinal += $"UBICACIÓN: {pj.NodoActual.Nombre}\n";

            // --- NUEVO: RENDERIZAR ESTADOS DEL ENTORNO ---
            if (pj.NodoActual.Estados.Count > 0)
            {
                textoFinal += "PELIGROS EN SALA: ";
                foreach (EstadoEntorno est in pj.NodoActual.Estados)
                {
                    textoFinal += $"<color=orange>[{est.ToString().ToUpper()}]</color> ";
                }
                textoFinal += "\n";
            }
            // ----------------

            // --- NUEVO: NIEBLA DE GUERRA EN CONEXIONES ---
            textoFinal += "CONEXIONES:\n";
            foreach (Nodo n in pj.NodoActual.Adyacentes)
            {
                if (n.Descubierto)
                {
                    textoFinal += $"> {n.Nombre}\n";
                }
                else
                {
                    textoFinal += $"> ??? (Inexplorado)\n";
                }
            }
            textoFinal += "--------------------\n";
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

        // --- MOSTRAR EQUIPAMIENTO ADICIONAL (RELIQUIAS, CHALECOS, ETC.) ---
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

        // --- BLOQUE DE LOS POIs ---
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