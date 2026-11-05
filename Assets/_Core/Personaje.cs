using System.Collections.Generic;
using System.Linq; // Necesario para elegir un atributo al azar
using UnityEngine;

public enum TipoDado
{
    d4 = 4,
    d6 = 6,
    d8 = 8,
    d10 = 10,
    d12 = 12
}

// NUEVO: Estados de salud mental/física
public enum EstadoMental
{
    Sano,
    Estresado,
    Ansiedad,
    Muerto
}

public class Personaje
{
    public string Nombre { get; private set; }
    public int VidaMaxima { get; private set; }
    public int VidaActual { get; private set; }
    public bool EstaMuerto { get; private set; }

    // NUEVO: Baliza de locura
    public EstadoMental EstadoPsicologico { get; private set; }

    public int AccionesMaximas { get; private set; }
    public int AccionesActuales { get; private set; }
    public Nodo NodoActual { get; private set; }

    public Dictionary<string, TipoDado> Atributos { get; private set; }
    public Dictionary<SlotEquipamiento, Item> Inventario { get; private set; }
    public List<Item> EquipamientoAdicional { get; private set; }

    public Personaje(string nombre, int vidaMaxima)
    {
        Nombre = nombre;
        VidaMaxima = vidaMaxima;
        VidaActual = vidaMaxima;
        EstaMuerto = false;
        EstadoPsicologico = EstadoMental.Sano; // Nace completamente cuerdo e ileso

        Atributos = new Dictionary<string, TipoDado>();
        AccionesMaximas = 3;
        AccionesActuales = AccionesMaximas;
        Inventario = new Dictionary<SlotEquipamiento, Item>();
        EquipamientoAdicional = new List<Item>();
    }

    public void AsignarAtributo(string nombreAtributo, TipoDado dado)
    {
        if (Atributos.ContainsKey(nombreAtributo))
        {
            Atributos[nombreAtributo] = dado;
        }
        else
        {
            Atributos.Add(nombreAtributo, dado);
        }
    }

    // ==========================================
    // SISTEMA RATCHET: GESTIÓN DE DAÑO Y ESTADOS
    // ==========================================
    public void RecibirDano(int cantidad)
    {
        if (EstaMuerto) return;

        VidaActual -= cantidad;

        // Si la vida cae a 0 o menos, interceptamos la muerte
        if (VidaActual <= 0)
        {
            EmpeorarEstadoMental();
        }
    }

    private void EmpeorarEstadoMental()
    {
        if (EstadoPsicologico == EstadoMental.Sano)
        {
            EstadoPsicologico = EstadoMental.Estresado;
            AplicarTraumaYBuff();
        }
        else if (EstadoPsicologico == EstadoMental.Estresado)
        {
            EstadoPsicologico = EstadoMental.Ansiedad;
            AplicarTraumaYBuff();
        }
        else if (EstadoPsicologico == EstadoMental.Ansiedad)
        {
            EstadoPsicologico = EstadoMental.Muerto;
            VidaActual = 0;
            EstaMuerto = true;
            Debug.Log($"{Nombre} ha muerto definitivamente.");
        }
    }

    private void AplicarTraumaYBuff()
    {
        // 1. EL COSTO: Reducimos la Vida Máxima un 35% y curamos al nuevo tope
        VidaMaxima = Mathf.Max(1, Mathf.RoundToInt(VidaMaxima * 0.65f));
        VidaActual = VidaMaxima;

        // 2. EL BENEFICIO: Mejora de Step-Dice
        MejorarAtributoAlAzar();

        Debug.Log($"{Nombre} entra en estado de {EstadoPsicologico}. Su vida máxima cae a {VidaMaxima}, pero sus habilidades se agudizan.");
    }

    // MÉTODOS DE MEJORA DE DADOS (Step-Dice)
    private void MejorarAtributoAlAzar()
    {
        if (Atributos.Count == 0) return;

        // Escogemos un atributo aleatorio para buffear (Temporal hasta que hagamos la UI)
        string atributoAleatorio = Atributos.Keys.ElementAt(Random.Range(0, Atributos.Count));
        MejorarAtributo(atributoAleatorio);
    }

    public bool MejorarAtributo(string nombreAtributo)
    {
        if (!Atributos.ContainsKey(nombreAtributo)) return false;

        TipoDado dadoActual = Atributos[nombreAtributo];
        TipoDado nuevoDado = dadoActual;

        // Escalada Step-Dice del GDD
        switch (dadoActual)
        {
            case TipoDado.d4: nuevoDado = TipoDado.d6; break;
            case TipoDado.d6: nuevoDado = TipoDado.d8; break;
            case TipoDado.d8: nuevoDado = TipoDado.d10; break;
            case TipoDado.d10: nuevoDado = TipoDado.d12; break;
            case TipoDado.d12: return false; // Ya ha llegado al máximo (humano) posible
        }

        Atributos[nombreAtributo] = nuevoDado;
        return true;
    }

    // ==========================================
    // MOTOR DE RESOLUCIÓN Y ACCIONES
    // ==========================================
    public int RealizarTirada(string nombreAtributo)
    {
        if (!Atributos.ContainsKey(nombreAtributo))
        {
            Debug.LogError($"Error: El personaje {Nombre} no tiene el atributo {nombreAtributo}");
            return 0;
        }

        int caras = (int)Atributos[nombreAtributo];
        int resultadoTotal = 0;
        int tiradaActual = 0;

        do
        {
            tiradaActual = Random.Range(1, caras + 1);
            resultadoTotal += tiradaActual;
        } while (tiradaActual == caras);

        return resultadoTotal;
    }

    public bool GastarAccion()
    {
        if (AccionesActuales > 0)
        {
            AccionesActuales--;
            return true;
        }
        return false;
    }

    public void RestaurarAcciones()
    {
        if (!EstaMuerto)
        {
            AccionesActuales = AccionesMaximas;
        }
    }

    // ==========================================
    // EQUIPAMIENTO Y ENTORNO
    // ==========================================
    public bool EquiparItem(Item nuevoItem)
    {
        if (nuevoItem.SlotRequerido == SlotEquipamiento.ManoDerecha || nuevoItem.SlotRequerido == SlotEquipamiento.ManoIzquierda)
        {
            if (Inventario.ContainsKey(nuevoItem.SlotRequerido))
            {
                Inventario[nuevoItem.SlotRequerido] = nuevoItem;
            }
            else
            {
                Inventario.Add(nuevoItem.SlotRequerido, nuevoItem);
            }
            return true;
        }
        else if (nuevoItem.SlotRequerido == SlotEquipamiento.HuecoEquipo)
        {
            if (EquipamientoAdicional.Count < 3)
            {
                EquipamientoAdicional.Add(nuevoItem);
                return true;
            }
            else
            {
                Debug.LogWarning("Inventario lleno: No caben más reliquias o equipo.");
                return false;
            }
        }
        return false;
    }

    public void EstablecerUbicacionInicial(Nodo nodoInicial)
    {
        NodoActual = nodoInicial;
        NodoActual.DescubrirSala();
    }

    public void CambiarDeNodo(Nodo nuevoNodo)
    {
        NodoActual = nuevoNodo;
        NodoActual.DescubrirSala();
    }
}