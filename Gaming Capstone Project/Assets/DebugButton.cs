using UnityEngine;

public class DebugButton : MonoBehaviour
{
    public void OnPress()
    {
        Debug.Log(gameObject.name + "'s Button was pressed!");
    }
}
