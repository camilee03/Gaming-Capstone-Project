using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyFadeScript : MonoBehaviour
{
    public Image Lobby;
    public float FadeInDuration, FadeOutDuration;
    public GameObject parentObj;
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
