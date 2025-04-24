using UnityEngine;

public class EnergyCoreButton : TaskButton
{
    char color;
    public EnergyCore core;
    public void setUpButton(char color)
    {
        this.color = color;
        Renderer r = gameObject.GetComponent<Renderer>();
        switch (color)
        {
            case 'r':
                r.material.color = Color.red;
                break;
            case 'g':
                r.material.color = Color.green;
                break;
            case 'b':
                r.material.color = Color.blue;
                break;
            case 'y':
                r.material.color = Color.yellow;
                break;
        }
    }
    public override void DoSomething()
    {
        core.PushButton(color);
    }
}
