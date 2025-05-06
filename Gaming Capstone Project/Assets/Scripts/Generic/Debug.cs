using UnityEngine;

public class DebugGen : MonoBehaviour
{
    public bool doDebug = false;
    public int seed;

    public static DebugGen Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this);
        }
        else { Instance = this; }

    }

    private void Start()
    {
        GameObject.DontDestroyOnLoad(this);
    }
}
