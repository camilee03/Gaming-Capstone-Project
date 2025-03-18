using TMPro;
using UnityEngine;

public class StaminaTracker : MonoBehaviour
{

    public PlayerController controller;
    public TMP_Text text; 

    private void Start()
    {
        controller.staminaTracker = this;
        text.text = controller.maxStamina + " / " + controller.maxStamina;
    }
    public void UpdateStamina(float Stamina)
    {
        text.text = Stamina.ToString("F0") + " / " + controller.maxStamina;

    }
}
