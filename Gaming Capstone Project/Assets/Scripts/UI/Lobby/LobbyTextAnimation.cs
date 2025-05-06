using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyTextAnimation : MonoBehaviour
{
    public TMP_Text text;
    Transform initpos;

    private void Start()
    {
        initpos = text.transform;
    }
    public void ShowError(int ErrorCode)
    {
        switch (ErrorCode)
        {
            case 0: //Not Host
                text.text = "Only the host can select start!";
                Animate();
                break;
            case 1://Not Enough Players
                text.text = "Not enough players!";
                Animate();
                break;

        }
    }

    private void Animate()
    {
        text.DOFade(1, 1.5f).OnComplete(() =>
        {
            text.DOFade(0f, 1.5f);

        });
        text.gameObject.transform.DOMoveY(100, 3.1f).OnComplete(() =>
        {
            text.gameObject.transform.DOMoveY(0, 0.1f);
        });
    }
}
