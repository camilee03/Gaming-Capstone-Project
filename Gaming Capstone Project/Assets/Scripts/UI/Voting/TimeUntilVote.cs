using TMPro;
using UnityEngine;

public class TimeUntilVote : MonoBehaviour
{
    public TMP_Text TextField;

    private void Update()
    {
        if (GameController.Instance != null)
        {
            int totalSeconds = GameController.Instance.secondsRemainingUntilVote.Value;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            TextField.text = $"{minutes}:{seconds:00}";
        }
    }
}

