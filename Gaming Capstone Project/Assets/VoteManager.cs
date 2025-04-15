using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class VoteManager : NetworkBehaviour
{
    public GameController Controller;
    public GameObject Colorbutton;
    public VerticalLayoutGroup VerticalLayoutGroup;
    private List<GameObject> buttons;

    private void OnEnable()
    {
        Controller = GameController.Instance;
        buttons = new List<GameObject>();   
    }
    public void ClearButtons()
    {
        foreach (GameObject button in buttons)
        {
            Destroy(button);
        }
    }
    public void CreateColorButtons()
    {
        foreach (Transform child in VerticalLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (int color in Controller.usedColors)
        {
            GameObject button = Instantiate(Colorbutton, VerticalLayoutGroup.transform);
            button.GetComponent<Image>().color = Controller.getColorByIndex(color);

            int capturedColor = color;
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnColorButtonClicked(capturedColor);
            });
            buttons.Add(button);
        }
    }

    

    private void OnColorButtonClicked(int colorIndex)
    {
        Debug.Log($"[VoteManager] Color {colorIndex} button clicked.");

        Controller.votesCasted.Add(colorIndex);
        Controller.ReceiveVote(colorIndex);
    }
}
