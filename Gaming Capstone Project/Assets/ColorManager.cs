using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    Dictionary<int, Color> ColorLibrary = new Dictionary<int, Color>();
    public Renderer hazmatSuit1, hazmatSuit2;
    private void Start()
    {
        ColorLibrary.Add(1,Color.HSVToRGB(0/360f,1,1)); //red
        ColorLibrary.Add(2, Color.HSVToRGB(25 / 360f, 1, 1));//orange
        ColorLibrary.Add(3, Color.HSVToRGB(50 / 360f, 1, 1));//yellow
        ColorLibrary.Add(4, Color.HSVToRGB(110 / 360f, 1, 1));//green
        ColorLibrary.Add(5, Color.HSVToRGB(180 / 360f, 1, 1));//teal
        ColorLibrary.Add(6, Color.HSVToRGB(210 / 360f, 1, 1));//blue
        ColorLibrary.Add(7, Color.HSVToRGB(280 / 360f, 1, 1));//purple
        ColorLibrary.Add(8, Color.HSVToRGB(310 / 360f, 1, 1));//pink
        ColorLibrary.Add(9, Color.HSVToRGB(0,0, 1));//white
        ColorLibrary.Add(10, Color.HSVToRGB(0,0, 0.5f));//gray
        ColorLibrary.Add(11, Color.HSVToRGB(0,0, 0.1f));//black
        ColorLibrary.Add(12, Color.HSVToRGB(30 / 360f, 0.9f, 4f));//brown
    }

    public void ChangeSuitColor(int index)
    {
        hazmatSuit1.material.SetColor("_Color", ColorLibrary[index]);
        hazmatSuit2.material.SetColor("_Color", ColorLibrary[index]);
    }
}
