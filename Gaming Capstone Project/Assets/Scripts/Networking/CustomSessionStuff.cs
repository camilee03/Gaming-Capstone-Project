using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CustomSessionStuff : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button[] buttons;
    public void EnterSession()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string finalString = "";

        for (int i = 0; i < 8; i++)  //create a random 8 digit string
        {
            finalString += chars[Mathf.RoundToInt(Random.Range(0, chars.Length))];
        }
        inputField.text = finalString;
        inputField.onEndEdit.Invoke(finalString);
        if (DebugGen.Instance.doDebug) { Debug.Log("Creating Session with the name: " + finalString); }
        foreach (Button button in buttons)
        {
            button.enabled = false;
        }
    }
}