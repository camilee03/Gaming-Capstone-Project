using UnityEngine;

public class ObjectData : MonoBehaviour
{
    bool isPaired;
    GameObject pairedObject;
    SerializableDict dictionary;
    Animator animator;

    private void Awake() 
    {
        animator = GetComponent<Animator>();
    }

    public void SetActiveAnimationState(bool state)
    {
        //animator.SetBool
    }

    public bool GetActiveAnimationState()
    {
        //animator.getBool(
        return true;
    }
}
