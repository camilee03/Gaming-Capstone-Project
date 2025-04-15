using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
public class DOSManager : NetworkBehaviour
{
    public static DOSManager Instance { get; private set; }
    public TMP_InputField InputField;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
