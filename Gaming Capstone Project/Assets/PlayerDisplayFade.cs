using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections;
using Unity.Netcode;

public class PlayerDisplayFade : NetworkBehaviour
{
    public Image image;

    public GameObject DopplesWinScreen;
    public GameObject ScientistsWinScreen;

    private void disableAllOtherChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) != null && transform.GetChild(i).gameObject == image.gameObject)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }

    }

    [ClientRpc]
    public void ScientistWinClientRpc()
    {
        image.DOFade(1f, 2.5f).OnComplete(() =>
        {
            disableAllOtherChildren();
            ScientistsWinScreen.SetActive(true);
            StartCoroutine(WaitThenFadeOut());
        });
    }

    [ClientRpc]
    public void DoppleWinClientRpc()
    {
        image.DOFade(1f, 2.5f).OnComplete(() =>
        {
            disableAllOtherChildren();
            DopplesWinScreen.SetActive(true);
            StartCoroutine(WaitThenFadeOut());
        });
    }

    private IEnumerator WaitThenFadeOut()
    {
        yield return new WaitForSeconds(5f);
        image.DOFade(0f, 2.5f).OnComplete(() =>
        {
        });
    }
}
