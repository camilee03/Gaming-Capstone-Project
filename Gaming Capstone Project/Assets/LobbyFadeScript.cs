using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyFadeScript : MonoBehaviour
{
    public CanvasGroup Lobby;
    public float FadeInDuration, FadeOutDuration;
    public GameObject parentObj;
    public Slider slider;
    private void Start()
    {
        FadeOut();
    }
    public void FadeIn()
    {
        Lobby.DOFade(1f, FadeInDuration);
    }

    public void LevelFade()
    {
        slider.DOValue(1, 5);

        Lobby.DOFade(1f, FadeInDuration).OnComplete(() =>
        {
            StartCoroutine(WaitThenFadeOut());
        });
    }

    private IEnumerator WaitThenFadeOut()
    {
        yield return new WaitForSeconds(5f);
        Lobby.DOFade(0f, FadeOutDuration).OnComplete(() =>
        {
            parentObj.SetActive(false);
        }); 
    }

    public void FadeOut()
    {
        Lobby.DOFade(0f, FadeOutDuration);
    }




}
