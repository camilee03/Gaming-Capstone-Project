using UnityEngine;
using System.Collections;

public class TaskButton : MonoBehaviour
{
    public Animator anim;
    public bool OneTimePress;
    bool toggled;
    public float pressDownTime = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (anim.GetBool("Toggled") && !toggled)
        {
            toggled = true;
            DoSomething();
            transform.tag = "ToggledButton";
            if (!OneTimePress)
            {
                StartCoroutine(UntoggleButton());
            }
        }
    }
    public virtual void DoSomething()
    {
        Debug.Log(gameObject.name + " is doing something.");
    }
    IEnumerator UntoggleButton()
    {
        yield return new WaitForSeconds(pressDownTime / 2);
        anim.SetBool("Toggled", false);
        yield return new WaitForSeconds(pressDownTime / 2);
        toggled = false;
        transform.tag = "Button";
    }
}
