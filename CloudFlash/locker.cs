using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

public class Locker
{
    List <Part> Parts {get; set;} = new List<Part>();
    public decimal TotalPrice 
    {
        get
        {
           return Parts.Sum(part => part.ClientPrice); 
        }
    }
    public float TotalHeight
    {
        get
        {
            return Parts.Sum(part => part.Height);
        }
    }
    public float TotalWidth
    {
        get
        {
            return Parts.Max(part => part.Width);
        }
    }
    public float TotalDepth
    {
        get
        {
            return Parts.Max(part => part.Depth);
        }
    }
}