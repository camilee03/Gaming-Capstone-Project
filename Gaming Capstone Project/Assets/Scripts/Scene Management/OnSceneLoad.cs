using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class OnSceneLoad : NetworkBehaviour
{
    public UnityEvent Events;
    private void Start()
    {
            Events.Invoke();
    }
}
