using System.Drawing;

public class Part
{
    public string Code {get; set;} = string.Empty;
    public string Kind {get; set;} = string.Empty;
    public string Color {get; set;} = string.Empty;
    public float Height {get; set;}
    public float Depth {get; set;}
    public float Width {get; set;}
    public decimal ClientPrice {get; set;} 
    public int InStock {get; set;}
    public int MinStock {get; set;}
    public int NbPartsByLocker {get; set;}
}


// pour les dimensions on pourrait utiliser une matrice et placer les modules dans la matrice,
//ca pourrait nous faciliter la tache pour les dommensions 