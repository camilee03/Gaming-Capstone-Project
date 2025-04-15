using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class VotingTimer : MonoBehaviour
{
    public TMP_Text TextField;

    private void Update()
    {
        if (GameController.Instance != null)
        {


            TextField.text = GameController.Instance.TimeLeftInVoting.ToString() ;
        }
    }
}

