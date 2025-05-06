using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class CodeOnlyInLobby : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1.5f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main" && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad);
        }
    }

    private void Update()
    {
     if(GameController.Instance.hostSelectedStart)
        {
            gameObject.SetActive(false);
        }
    }
}
