using UnityEngine;

public class ToLobbyBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == 3)
        {
            GameController.Instance.RespawnInLobby(other.gameObject);
        }
    }
}

