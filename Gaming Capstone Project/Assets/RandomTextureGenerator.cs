using UnityEngine;
using UnityEngine.UI;

public class RandomTextureGenerator : MonoBehaviour
{
    public RawImage rawImage; 

    void Start()
    {
        GenerateRandomTexture();
    }

    void GenerateRandomTexture()
    {
        Texture2D texture = new Texture2D(4, 4);
        texture.filterMode = FilterMode.Point; 

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Color color = Random.value > 0.5f ? Color.white : Color.black;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(); 

        rawImage.texture = texture;
    }
}
