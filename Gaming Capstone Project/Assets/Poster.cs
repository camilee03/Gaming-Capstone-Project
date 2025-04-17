using UnityEngine;

public class Poster : MonoBehaviour
{
    public Texture2D[] posterTexs;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Renderer r = gameObject.GetComponent<Renderer>();
        Texture2D tex = posterTexs[Mathf.RoundToInt(Random.Range(0, posterTexs.Length))];
        r.material.mainTexture = tex;
    }
}
