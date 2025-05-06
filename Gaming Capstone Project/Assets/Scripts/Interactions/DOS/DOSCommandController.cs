using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using WebSocketSharp;

public class DOSCommandController : MonoBehaviour
{
    public static DOSCommandController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public string HandleCommand(string command)
    {
        command = command.ToLower();
        string target = "";
        if (command.Contains(" "))
        {
            target = command.Substring(command.IndexOf(" ") + 1);
            command = command.Substring(0, command.IndexOf(" "));
        }

        switch (command)
        {
            case "/spook":
                break;
            case "/help":
                return "/help";
            default:
                if (target.Length > 0)
                {
                    //find current room instead of room with name
                    (bool hasRoom, Room currentRoom) = FindRoomWithName(target);
                    if (!hasRoom || currentRoom.roomName == null) { return "ERROR: code 6070\n No such room name found."; }

                    switch (command)
                    {
                        case "/comms":
                            List<AudioSource> speakerList = currentRoom.GetAllSpeakers();
                            if (speakerList.Count != 0)
                            {
                                foreach (AudioSource speaker in currentRoom.GetAllSpeakers())
                                {
                                    // Play person's voice
                                }
                            }
                            else { return "ERROR: code ___\n No speakers found in " + currentRoom.roomName; }
                            break;
                        case "/fan":
                            List<NetworkAnimator> fanList = currentRoom.GetAllFans();
                            if (fanList.Count != 0)
                            {
                                foreach (NetworkAnimator fan in fanList)
                                {
                                    fan.Animator.SetBool("Toggle", !fan.Animator.GetBool("Toggle"));
                                }
                            }
                            else { return "ERROR: code ___\n No fans found in " + currentRoom.roomName; }
                            break;
                        case "/jazz":
                            speakerList = currentRoom.GetAllSpeakers();
                            if (speakerList.Count != 0)
                            {
                                foreach (AudioSource speaker in currentRoom.GetAllSpeakers())
                                {
                                    if (!speaker.isPlaying) { speaker.Play(); }
                                    else { speaker.Stop(); }
                                }
                            }
                            else { return "ERROR: code ___\n No speakers found in " + currentRoom.roomName; }
                            break;
                        case "/lights":
                            foreach (Light light in currentRoom.GetAllLights())
                            {
                                light.enabled = !light.enabled;
                            }
                            break;
                        default:
                            return "ERROR: code 117\nNot a viable code";
                    }
                }
                else
                {
                    switch (command)
                    {
                        case "/comms":
                        case "/fan":
                        case "/jazz":
                        case "/lights":
                            return "ERROR: code 59\n No room name entered";
                        default:
                            return "ERROR: code 117\nNot a viable code";
                    }
                }
                break;
        }

        return "";
    }

    private (bool, Room) FindRoomWithName(string target)
    {
        foreach (Room room in RoomManager.Instance.rooms)
        {
            if (room.roomName.Replace(" ", "").ToLower().Equals(target.Replace(" ", "").ToLower()))
            {
                return (true, room);
            }
        }

        return (false, null);
    }

}
