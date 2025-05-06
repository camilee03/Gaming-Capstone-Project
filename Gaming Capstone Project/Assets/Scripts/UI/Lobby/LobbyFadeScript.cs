using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyFadeScript : NetworkBehaviour
{
    public CanvasGroup Lobby;
    public float FadeInDuration, FadeOutDuration;
    public GameObject parentObj;
    public Slider slider;
    public GameObject LobbyRender;
    private void Start()
    {
        FadeOut();
    }
    public void FadeIn()
    {
        Lobby.DOFade(1f, FadeInDuration);
    }
    [ClientRpc]
    public void LevelFadeClientRpc()
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
        LobbyRender.gameObject.SetActive(false);

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
