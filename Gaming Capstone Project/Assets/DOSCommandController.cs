using UnityEngine;

public class DOSCommandController : MonoBehaviour
{
    public static DOSCommandController Instance { get; private set; }

    [SerializeField] private Room[] Rooms;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void HandleCommand(string command)
    {
        command = command.ToLower();
        string target = "";
        if (command.Contains(" "))
        {
            target = command.Substring(command.IndexOf(" ") + 1);
            command = command.Substring(0, command.IndexOf(" "));
        }

        if (target.Length > 0)
        {
            switch (command) //Commands with Target Rooms
            {
                case "/lights":
                    Room currentRoom = FindRoomWithName(target);
                    if (currentRoom != null)
                    {
                        // currentRoom.SendCommand(ToggleLights);
                    }
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
                default:
                    Debug.Log("Not a Command");
                    break;
            }
        }
        else
        {
            switch (command) //Commands with Target Rooms
            {
                case "/lights":
                    //find current room instead of room with name
                    Room currentRoom = FindRoomWithName(target);
                    if (currentRoom != null)
                    {
                        // currentRoom.SendCommand(ToggleLights);
                    }
                    break;
                case "/fan":
                    break;
                case "/jazz":
                    Debug.Log("Imagine Jazz rn");
                    break;
                case "/spook":
                    break;
                case "/map":
                    break;
                case "/help":
                    break;
                default:
                    Debug.Log("Not a Command");
                    break;
            }
        }
    }

    private Room FindRoomWithName(string target)
    {
        //foreach (Room room in Rooms)
        //{
        //    if (room.name.ToLower() == target.ToLower())
        //    {
        //        return room;
        //    }
        //}
        return null;
    }
}
