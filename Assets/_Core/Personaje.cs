using System.Collections.Generic;
using UnityEngine; // Lo usamos únicamente para el Random.Range y el Debug

// Definimos los tipos de dado según tu GDD (Step-Dice)
public enum TipoDado
{
    d4 = 4,
    d6 = 6,
    d8 = 8,
    d10 = 10,
    d12 = 12
}

public class Personaje
{
    // VARIABLES DE ESTADO Y SUPERVIVENCIA
    public string Nombre { get; private set; }
    public int VidaMaxima { get; private set; }
    public int VidaActual { get; private set; }
    public bool EstaMuerto { get; private set; }

    public int AccionesMaximas { get; private set; }
    public int AccionesActuales { get; private set; }

    public Nodo NodoActual { get; private set; }

    // DICCIONARIO DE ATRIBUTOS (Protegido contra sobreescritura externa)
    public Dictionary<string, TipoDado> Atributos { get; private set; }
    public Dictionary<SlotEquipamiento, Item> Inventario { get; private set; }

    public List<Item> EquipamientoAdicional { get; private set; }

    // CONSTRUCTOR
    public Personaje(string nombre, int vidaMaxima)
    {
        Nombre = nombre;
        VidaMaxima = vidaMaxima;

        // Al instanciar, el personaje nace con la vida al máximo y vivo
        VidaActual = vidaMaxima;
        EstaMuerto = false;

        Atributos = new Dictionary<string, TipoDado>();

        AccionesMaximas = 3; // Según el GDD, son 3 acciones por turno
        AccionesActuales = AccionesMaximas;

        Inventario = new Dictionary<SlotEquipamiento, Item>();
        EquipamientoAdicional = new List<Item>();
    }

    // MÉTODO PARA CARGAR LOS DADOS DESDE EL JSON (GameManager)
    public void AsignarAtributo(string nombreAtributo, TipoDado dado)
    {
        // Si el atributo ya existe lo actualizamos; si no, lo añadimos de cero
        if (Atributos.ContainsKey(nombreAtributo))
        {
            Atributos[nombreAtributo] = dado;
        }
        else
        {
            Atributos.Add(nombreAtributo, dado);
        }
    }

    // MÉTODO PARA GESTIONAR EL DAÑO (La Ley de Inmortalidad Negativa)
    public void RecibirDano(int cantidad)
    {
        if (EstaMuerto) return; // Si ya está muerto, ignoramos el daño post-mortem

        VidaActual -= cantidad;

        if (VidaActual <= 0)
        {
            VidaActual = 0;
            EstaMuerto = true;
        }
    }

    // EL MOTOR MATEMÁTICO: TIRADAS Y DADOS EXPLOSIVOS (Punto 5 del GDD)
    public int RealizarTirada(string nombreAtributo)
    {
        // Seguro por si nos equivocamos al escribir el nombre del atributo ("Arcano" vs "arcano")
        if (!Atributos.ContainsKey(nombreAtributo))
        {
            Debug.LogError($"Error: El personaje {Nombre} no tiene el atributo {nombreAtributo}");
            return 0;
        }

        int caras = (int)Atributos[nombreAtributo]; // Extraemos el número de caras (ej. 8)
        int resultadoTotal = 0;
        int tiradaActual = 0;

        // Bucle do-while para gestionar las explosiones de dados
        do
        {
            // Random.Range con enteros es exclusivo en el máximo, por eso usamos 'caras + 1'
            tiradaActual = Random.Range(1, caras + 1);
            resultadoTotal += tiradaActual;

        } while (tiradaActual == caras); // Si sacas el máximo (ej. un 8 en un d8), explota y vuelve a tirar

        return resultadoTotal;
    }

    public bool GastarAccion()
    {
        if (AccionesActuales > 0)
        {
            AccionesActuales--;
            return true; // Se pudo gastar
        }
        return false; // No le quedan acciones
    }

    public void RestaurarAcciones()
    {
        // Solo restauramos acciones si el personaje sigue vivo
        if (!EstaMuerto)
        {
            AccionesActuales = AccionesMaximas;
        }
    }

    // 2. NUEVO: MÉTODO PARA EQUIPAR OBJETOS
    public bool EquiparItem(Item nuevoItem)
    {
        // Si va a las manos, usamos el Diccionario (solo cabe 1 por mano)
        if (nuevoItem.SlotRequerido == SlotEquipamiento.ManoDerecha || nuevoItem.SlotRequerido == SlotEquipamiento.ManoIzquierda)
        {
            if (Inventario.ContainsKey(nuevoItem.SlotRequerido))
            {
                Inventario[nuevoItem.SlotRequerido] = nuevoItem; // Sobreescribe el que ya tenía
            }
            else
            {
                Inventario.Add(nuevoItem.SlotRequerido, nuevoItem);
            }
            return true;
        }
        // Si va al cuerpo, usamos la lista (máximo 3 según el GDD)
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
    }

    public void CambiarDeNodo(Nodo nuevoNodo)
    {
        NodoActual = nuevoNodo;
    }
}