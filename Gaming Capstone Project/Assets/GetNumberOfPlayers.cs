using TMPro;
using UnityEngine;

public class GetNumberOfPlayers : MonoBehaviour
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
    }
}
