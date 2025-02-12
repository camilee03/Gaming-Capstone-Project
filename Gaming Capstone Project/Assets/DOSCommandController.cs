using UnityEngine;

public class DOSCommandController : MonoBehaviour
{
   

    public void HandleCommand(string command)
    {
        
        command = command.ToLower();
        switch (command)
        {
            case "/lights":

                break;
            case "/fan":

                break;
            case "/jazz":
                Debug.Log("Imagine Jazz rn");
                break;
            case "/spook":

                break;
            case "/vote":

                break;
            case "/help":

                break;
            case "/diagnostic":

                break;
            case "/map":

                break;

            default:
                Debug.Log("Not a Command");
                return;
        }
    }
}
