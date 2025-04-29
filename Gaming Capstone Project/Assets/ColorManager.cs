using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour
{

    public Color[] colors = {
        Color.HSVToRGB(0/360f,1,1), //Red
        Color.HSVToRGB(25/360f,1,1), //Orange
        Color.HSVToRGB(50/360f,1,1), //Yellow
        Color.HSVToRGB(110/360f,1,1), //Green
        Color.HSVToRGB(180/360f,1,1), //Teal
        Color.HSVToRGB(210/360f,1,1), //Blue
        Color.HSVToRGB(280/360f,1,1), //Purple
        Color.HSVToRGB(310/360f,.8f,1), //Pink
        Color.HSVToRGB(0,0,1), //White
        Color.HSVToRGB(0,0,.5f), //Gray
        Color.HSVToRGB(0,0,.1f), //Black
        Color.HSVToRGB(30/360f,.9f,.4f), //Brown
    };
    public Renderer hazmatSuit1, hazmatSuit2, indicator;

    public void ChangeSuitColor(int index)
    {
        if (index < colors.Length && index > -1)
        {
            hazmatSuit1.material.SetColor("_Color", colors[index]);
            hazmatSuit2.material.SetColor("_Color", colors[index]);
            if (indicator != null)
            {
                Debug.Log("Changing Color of Indicator");
                indicator.material.SetColor("_BaseColor", colors[index]);
            }
        }
    }
}
