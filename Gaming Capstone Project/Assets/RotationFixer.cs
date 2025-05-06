using UnityEngine;

public class RotationFixer : MonoBehaviour
{
    void OnEnable()
    {
        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
