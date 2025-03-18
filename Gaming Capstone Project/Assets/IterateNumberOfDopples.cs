using TMPro;
using UnityEngine;

public class IterateNumberOfDopples : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private GameController gameController;
    public TMP_Text Number;
    private int currentSelection = 1;
    void Start()
    {
        gameController = GameController.Instance;
        Number.text = "1";
    }

    public void IncreaseNumberOfDopples()
    {
        if(gameController.CanIncreaseDopples(currentSelection +1))
        {
            currentSelection++;
            gameController.SetNumberOfDopples(currentSelection);
            Number.text = currentSelection.ToString();

        }
        else return;
    }
    public void DecreaseNumberOfDopples()
    {
        if (currentSelection > 1)
        {
            currentSelection--;
            Number.text = currentSelection.ToString();
        }
        else return;
    }
}
