using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class DOSManager : NetworkBehaviour
{
    public static DOSManager Instance { get; private set; }
    public TMP_InputField InputField;
    public TMP_Text[] CommandLines;
    public GameObject ErrorPopup;
    public TMP_Text ErrorText;
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
