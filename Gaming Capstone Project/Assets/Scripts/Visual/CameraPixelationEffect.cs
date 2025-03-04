using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CameraPixelationEffect : MonoBehaviour
{
    public bool RenderPixelationEffect = true;
    public RenderTexture PlayerRenderTexture;
    public float scaling = 6;
    [SerializeField] private RawImage ViewPort;

    public Camera PlayerCam;



    void OnRectTransformDimensionsChange()
    {
        Debug.LogWarning("Window Resolution Changed");
        UpdateViewResolution();
    }

    void UpdateViewResolution()
    {
        if (RenderPixelationEffect)
        {
            PlayerRenderTexture = new RenderTexture((int)(Screen.width / scaling), (int)(Screen.height / scaling), PlayerRenderTexture.depth);
            PlayerCam.targetTexture = PlayerRenderTexture;
            ViewPort.texture = PlayerRenderTexture;
        }
        else
        {
            PlayerRenderTexture = new RenderTexture(Screen.width,Screen.height, PlayerRenderTexture.depth);
           PlayerCam.targetTexture = PlayerRenderTexture;
            ViewPort.texture = PlayerRenderTexture;
        }
    }
}
