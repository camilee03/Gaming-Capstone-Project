using Unity.Netcode;
using UnityEngine;

public class OnLoadSceneResetTransforms : NetworkBehaviour
{
    void Start()
    {
        GameController.Instance.SpawnInLobby();
    }


}
