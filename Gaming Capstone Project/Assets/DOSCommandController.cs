using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode.Components;
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
            //find current room instead of room with name
            Room currentRoom = FindRoomWithName(target);

            switch (command) //Commands with Target Rooms
            {
                case "/lights":
                    if (currentRoom != null)
                    {
                        foreach (Light light in currentRoom.GetAllLights())
                        {
                            light.enabled = !light.enabled;
                        }
                    }
                    break;

                case "/fan":
                    if (currentRoom != null)
                    {
                        List<NetworkAnimator> fanList = currentRoom.GetAllFans();
                        if (fanList != null)
                        {
                            foreach (NetworkAnimator fan in fanList)
                            {
                                fan.Animator.SetBool("Toggle", !fan.Animator.GetBool("Toggle"));
                            }
                        }
                    }
                    break;
                case "/jazz":
                    if (currentRoom != null)
                    {
                        foreach (AudioSource speaker in currentRoom.GetAllSpeakers())
                        {
                            if (!speaker.isPlaying) { speaker.Play(); } 
                            else { speaker.Stop(); }
                        }
                    }
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
            switch (command) //Commands w/o Target Rooms
            {
                case "/lights":
                case "/fan":
                case "/jazz":
                    Debug.Log("Enter a room");
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

    private void CommandSwitchStates(string command)
    {

    }

    private Room FindRoomWithName(string target)
    {
        foreach (Room room in Rooms)
        {
            if (room.name.ToLower() == target.ToLower())
            {
                return room;
            }
        }
        return null;
    }
}
