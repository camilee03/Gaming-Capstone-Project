using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashLight : NetworkBehaviour
{
    bool flashlightEnabled = false;
    [SerializeField] GameObject flashlight;
    public void FlashLightToggle(InputAction.CallbackContext context)
    {
        if (context.performed && IsLocalPlayer)
        {
            Toggle(); // always toggles locally

            if (IsOwner)
            {
                if (IsServer) { FlashlightServerRpc(); }
                else { FlashlightServerRpc(); }
            }
        }
        //flashlight.GetComponent<NetworkObject>().enabled = flashlightEnabled;
    }

    [ClientRpc]
    private void FlashlightClientRpc()
    {
        Toggle();
        //FlashlightServerRpc();
        Debug.Log("Client");
    }

    [ServerRpc]
    private void FlashlightServerRpc()
    {
        Toggle();
        Debug.Log("Server");
    }

    private void Toggle()
    {
        flashlightEnabled = !flashlightEnabled;
        flashlight.SetActive(flashlightEnabled);
    }

}
