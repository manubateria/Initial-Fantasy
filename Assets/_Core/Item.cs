// Clasificamos qué tipo de objeto es
public enum CategoriaItem
{
    Arma,
    Consumible,
    Utilidad
}

// Definimos dónde se puede equipar (El GDD dice: Manos y 3 huecos de equipo)
public enum SlotEquipamiento
{
    ManoIzquierda,
    ManoDerecha,
    HuecoEquipo
}

public class Item
{
    public string Nombre { get; private set; }
    public CategoriaItem Categoria { get; private set; }
    public SlotEquipamiento SlotRequerido { get; private set; }

    // VARIABLES FLEXIBLES (El núcleo de tu genial idea)
    // Si es un arma, Atributo será "Fuerza" y Valor será el Daño.
    // Si es una poción, Atributo estará vacío y Valor será la Curación.
    public string AtributoUso { get; private set; }
    public int ValorEfecto { get; private set; }

    public Item(string nombre, CategoriaItem categoria, SlotEquipamiento slot, string atributo, int valor)
    {
        Nombre = nombre;
        Categoria = categoria;
        SlotRequerido = slot;
        AtributoUso = atributo;
        ValorEfecto = valor;
    }
}