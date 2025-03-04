using UnityEngine;
using UnityEngine.InputSystem;

public class FlashLight : MonoBehaviour
{
    bool flashlightEnabled = false;
    [SerializeField] private Light flashlight;
    public void FlashLightToggle(InputAction.CallbackContext context)
    {
        if(context.performed) flashlightEnabled = !flashlightEnabled;
        flashlight.enabled = flashlightEnabled;
    }





}
