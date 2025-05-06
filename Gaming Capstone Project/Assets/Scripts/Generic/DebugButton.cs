using UnityEngine;

public class DebugButton : MonoBehaviour
{
    public void OnPress()
    {
        if (DebugGen.Instance.doDebug) Debug.Log(gameObject.name + "'s Button was pressed!");
    }
}
