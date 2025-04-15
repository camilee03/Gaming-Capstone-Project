using UnityEngine;

public class Customizer : MonoBehaviour
{
    public Renderer hazmatSuit, selfHazmatSuit;
    public int id;
    Color[] colors = {
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
    private void Start()
    {
        ChangeColor(id);
    }
    public void ChangeColor(int id)
    {
        hazmatSuit.material.SetColor("_Color", colors[id]);
        selfHazmatSuit.material.SetColor("_Color", colors[id]);
    }
}
