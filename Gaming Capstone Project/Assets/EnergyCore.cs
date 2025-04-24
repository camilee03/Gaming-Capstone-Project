using UnityEngine;
using System.Collections.Generic;

public class EnergyCore : MonoBehaviour
{
    string generatedCode = "", inputtedCode = "";
    char[] colors = { 'r', 'g', 'b', 'y' };
    public Renderer[] expectedRenderer, receivedRenderer;
    bool listeningForCode = true;

    public EnergyCoreButton[] buttons;
    void GenerateCode()
    {
        for (int i = 0; i < 4; i++)
        {
            char color = colors[Mathf.RoundToInt(Random.Range(0, 4))];
            switch (color)
            {
                case 'r':
                    expectedRenderer[i].material.color = Color.red;
                    break;
                case 'b':
                    expectedRenderer[i].material.color = Color.blue;
                    break;
                case 'g':
                    expectedRenderer[i].material.color = Color.green;
                    break;
                case 'y':
                    expectedRenderer[i].material.color = Color.yellow;
                    break;
            }

            generatedCode += color;
        }
        Debug.Log("Generated Code:" + generatedCode); 
    }
    void RandomizeButtons()
    {
        List<char> colorsAsList = new List<char>();
        colorsAsList.Add('r'); colorsAsList.Add('g'); colorsAsList.Add('b'); colorsAsList.Add('y');
        int i = 0;
        while (colorsAsList.Count > 0)
        {
            char c = colorsAsList[Mathf.RoundToInt(Random.Range(0, colorsAsList.Count))];
            colorsAsList.Remove(c);
            buttons[i].setUpButton(c);
            i++;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateCode();
        ResetReceivedCode();
        RandomizeButtons();
    }
    public void ResetReceivedCode()
    {
        inputtedCode = "";
        foreach (Renderer r in receivedRenderer)
        {
            r.material.color = Color.white;
        }
    }

    public void PushButton(char c)
    {
        if (listeningForCode)
        {
            switch (c)
            {
                case 'r':
                    receivedRenderer[inputtedCode.Length].material.color = Color.red;
                    break;
                case 'g':
                    receivedRenderer[inputtedCode.Length].material.color = Color.green;
                    break;
                case 'b':
                    receivedRenderer[inputtedCode.Length].material.color = Color.blue;
                    break;
                case 'y':
                    receivedRenderer[inputtedCode.Length].material.color = Color.yellow;
                    break;
            }
            inputtedCode += c;
            if (inputtedCode.Length == 4)
            {
                if (inputtedCode == generatedCode)
                {
                    Debug.Log("Correct Code!"); //maybe play a correct sound effect
                    //finish the task here.
                    listeningForCode = false;
                }
                else
                {
                    Debug.Log("incorrect code."); //maybe play an error sound effect
                    ResetReceivedCode();
                }
            }
        }
    }
}
