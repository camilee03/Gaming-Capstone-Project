using UnityEngine;
using UnityEngine.InputSystem;

public class EscapeToPreviousMenu : MonoBehaviour
{

    public GameObject[] ElementsToEnable;
    public GameObject[] ElementsToDisable;
    public void Escape(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            foreach (GameObject element in ElementsToEnable)
            {
                element.SetActive(true);
            }
            foreach(GameObject element in ElementsToDisable)
            {
                element.SetActive(false);
            }
        }
    }
}
