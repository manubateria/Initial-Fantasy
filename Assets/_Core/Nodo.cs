using System.Collections.Generic;

public class Nodo
{
    public string Nombre { get; private set; }

    // Lista de las habitaciones a las que se puede ir desde aquí
    public List<Nodo> Adyacentes { get; private set; }

    public List<PuntoInteres> PuntosDeInteres { get; private set; }

    public Nodo(string nombre)
    {
        Nombre = nombre;
        Adyacentes = new List<Nodo>();
        PuntosDeInteres = new List<PuntoInteres>();
    }

    // MÉTODO PARA CONECTAR HABITACIONES
    public void ConectarCon(Nodo otroNodo)
    {
        if (!Adyacentes.Contains(otroNodo))
        {
            Adyacentes.Add(otroNodo);       // A conecta con B
            otroNodo.Adyacentes.Add(this);  // B conecta con A automáticamente
        }
    }
    public void AgregarPOI(PuntoInteres poi)
    {
        if (!PuntosDeInteres.Contains(poi))
        {
            PuntosDeInteres.Add(poi);
        }
    }
}