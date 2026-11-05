// NUEVO: Cómo se comporta si no escucha ni ve a nadie
public enum ComportamientoReposo
{
    Acechar, // Se queda quieto en su nodo
    Deambular // Se mueve a un nodo adyacente al azar
}

public class Enemigo
{
    public string Nombre { get; private set; }
    public int VidaMaxima { get; private set; }
    public int VidaActual { get; private set; }
    public int DefensaDC { get; private set; }
    public int DanoAtaque { get; private set; }
    public Nodo NodoActual { get; private set; }
    public bool EstaMuerto { get; private set; }

    // NUEVO: Variables de Inteligencia Artificial
    public int RangoAudicion { get; private set; }
    public ComportamientoReposo Reposo { get; private set; }

    public Enemigo(string nombre, int vida, int defensaDC, int danoAtaque, int rangoAudicion, ComportamientoReposo reposo)
    {
        Nombre = nombre;
        VidaMaxima = vida;
        VidaActual = vida;
        DefensaDC = defensaDC;
        EstaMuerto = false;
        DanoAtaque = danoAtaque;

        // Asignamos la nueva IA
        RangoAudicion = rangoAudicion;
        Reposo = reposo;
    }

    public void RecibirDano(int cantidad)
    {
        if (EstaMuerto) return;

        VidaActual -= cantidad;
        if (VidaActual <= 0)
        {
            VidaActual = 0;
            EstaMuerto = true;
        }
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