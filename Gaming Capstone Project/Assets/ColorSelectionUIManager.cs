using UnityEngine;

public class ColorSelectionUIManager : MonoBehaviour
{
    public SelectColorButton[] colorButtons;

    private void Start()
    {

        // Initial refresh
        RefreshAll();
    }

    /// <summary>
    /// Refreshes the visual state of all color buttons
    /// </summary>
    public void RefreshAll()
    {
        foreach (var button in colorButtons)
        {
            button.RefreshState();
        }
    }
}
