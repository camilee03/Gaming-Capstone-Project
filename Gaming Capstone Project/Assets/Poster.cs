using UnityEngine;
using Unity.Netcode;

public class Poster : NetworkBehaviour
{
    public Texture2D[] posterTexs;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int i = Mathf.RoundToInt(Random.Range(0, posterTexs.Length));
        if (IsOwner)
        {
            if (IsServer) { PosterClientRpc(i); }
            else { PosterServerRpc(i); }
        }
    }

    [ClientRpc]
    private void PosterClientRpc(int i)
    {
        SetPosterMat(i);
    }

    [ServerRpc]
    private void PosterServerRpc(int i)
    {
        PosterClientRpc(i);
    }

    private void SetPosterMat(int i)
    {
        Renderer r = gameObject.GetComponent<Renderer>();
        Texture2D tex = posterTexs[i];
        r.material.mainTexture = tex;
    }
}
