using UnityEngine;
using UnityEngine.UI;

public class LobbyColorButton : MonoBehaviour
{

    private Button buttonComponent;
    private Image buttonImage;
    private Color color;
    public int colorIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonComponent = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        color = buttonImage.color;
    }

    public void RefreshState()
    {
        if (GameController.Instance == null || buttonComponent == null) return;

        bool available = GameController.Instance.IsColorAvailable(colorIndex);
        buttonComponent.interactable = available;

        // Gray out the color if taken
        if (buttonImage != null)
        {
            if (available) buttonImage.color = color;
            else buttonImage.color = Color.gray;
            Debug.Log("RefreshedColor");
        }
    }
}
