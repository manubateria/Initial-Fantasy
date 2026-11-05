using System.Collections.Generic;



// NUEVO: Posibles estados alterados de la habitación
public enum EstadoEntorno
{
    Oscuro,
    Inundado,
    Ardiendo,
    Toxico,
    Derrumbado
}

public class Nodo
{
    public string Nombre { get; private set; }
    public List<Nodo> Adyacentes { get; private set; }
    public List<PuntoInteres> PuntosDeInteres { get; private set; }

    public bool Descubierto { get; private set; }
    public int NivelRuido { get; private set; }

    // NUEVO: Lista de estados actuales de la sala
    public List<EstadoEntorno> Estados { get; private set; }

    public Nodo(string nombre)
    {
        Nombre = nombre;
        Adyacentes = new List<Nodo>();
        PuntosDeInteres = new List<PuntoInteres>();
        Descubierto = false;
        NivelRuido = 0;
        Estados = new List<EstadoEntorno>(); // Al crearse, la sala está en estado "Normal"
    }

    public void ConectarCon(Nodo otroNodo)
    {
        if (!Adyacentes.Contains(otroNodo))
        {
            Adyacentes.Add(otroNodo);
            otroNodo.Adyacentes.Add(this);
        }
    }

    public void AgregarPOI(PuntoInteres poi)
    {
        if (!PuntosDeInteres.Contains(poi))
        {
            PuntosDeInteres.Add(poi);
        }
    }

    public void DescubrirSala()
    {
        Descubierto = true;
    }

    public void GenerarRuido(int cantidad)
    {
        NivelRuido += cantidad;
    }

    public void DisiparRuido()
    {
        if (NivelRuido > 0) NivelRuido--;
    }

    // ==========================================
    // NUEVO: GESTIÓN DE ENTORNO
    // ==========================================
    public void AnadirEstado(EstadoEntorno nuevoEstado)
    {
        if (!Estados.Contains(nuevoEstado))
        {
            Estados.Add(nuevoEstado);
        }
    }

    public void QuitarEstado(EstadoEntorno estadoAQuitar)
    {
        if (Estados.Contains(estadoAQuitar))
        {
            Estados.Remove(estadoAQuitar);
        }
    }

    public bool TieneEstado(EstadoEntorno estado)
    {
        return Estados.Contains(estado);
    }
}