using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GetNumberOfPlayers : NetworkBehaviour
{
    TMP_Text m_Text;
    GameController gameController;
    private void Start()
    {
        m_Text = GetComponent<TMP_Text>();
        gameController = GameController.Instance;
    }
    private void Update()
    {
        m_Text.text = gameController.GetNumberOfPlayers().ToString();
        m_Text.text = NetworkManager.ConnectedClients.Count.ToString();
    }
}
