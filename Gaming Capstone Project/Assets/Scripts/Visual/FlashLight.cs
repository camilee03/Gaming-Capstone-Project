using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashLight : NetworkBehaviour
{
    bool flashlightEnabled = false;
    [SerializeField] Light flashlight;

    public void FlashLightToggle(InputAction.CallbackContext context)
    {
        if (context.performed && IsLocalPlayer)
        {
            //Toggle(); // always toggles locally

            if (IsOwner)
            {
                if (IsServer) { FlashlightClientRpc(); }
                else { FlashlightServerRpc(); }
            }
        }
        //flashlight.GetComponent<NetworkObject>().enabled = flashlightEnabled;
    }

    [ClientRpc]
    private void FlashlightClientRpc()
    {
        Toggle();
        Debug.Log("Client");
    }

    [ServerRpc]
    private void FlashlightServerRpc()
    {
        FlashlightClientRpc();
        Debug.Log("Server");
    }

    private void Toggle()
    {
        Debug.Log("Toggling FlashLight");
        flashlightEnabled = !flashlightEnabled;
        flashlight.enabled = flashlightEnabled;
    }

}
