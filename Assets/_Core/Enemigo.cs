public class Enemigo
{
    public string Nombre { get; private set; }
    public int VidaMaxima { get; private set; }
    public int VidaActual { get; private set; }
    public int DefensaDC { get; private set; }

    public int DanoAtaque { get; private set; }

    public Nodo NodoActual { get; private set; }

    // NUEVO: La baliza que indica si el motor debe ignorar a este enemigo
    public bool EstaMuerto { get; private set; }

    public Enemigo(string nombre, int vida, int defensaDC, int danoAtaque)
    {
        Nombre = nombre;
        VidaMaxima = vida;
        VidaActual = vida;
        DefensaDC = defensaDC;
        EstaMuerto = false; // Al nacer, obviamente está vivo
        DanoAtaque = danoAtaque;
    }

    public void RecibirDano(int cantidad)
    {
        // Si ya está muerto, cortamos la ejecución para que no reciba más daño post-mortem
        if (EstaMuerto) return;

        VidaActual -= cantidad;

        // Tope lógico: Si la vida cae por debajo de cero, la dejamos en cero y declaramos la muerte
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