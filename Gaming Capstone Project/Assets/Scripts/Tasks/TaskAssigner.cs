using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TaskAssigner : MonoBehaviour
{
    // Task containers
    List<RoomTask> taskList;
    List<RoomTask> assignedTasks = new List<RoomTask>();
    bool[] finishedTasks;
    int numTasks = 3;

    // Task display
    [SerializeField] TMP_Text goalText;
    [SerializeField] TMP_Text tasksCompleted;
    TaskManager taskManager;

    public bool start = false; // toggle through another function
    bool donow = false;

    private void Start()
    {
    }


    private void Update()
    {
        if (start)
        {
            AssignTasks();
            start = false;
            donow = true;
        }
        else if (donow)
        {
            UpdateTasks();   
        }
    }

    void UpdateTasks()
    {
        int numTasksFinished = 0;

        for (int i = 0; i < assignedTasks.Count; i++)
        {
            if (!finishedTasks[i])
            {
                switch (assignedTasks[i].type)
                {
                    case TaskType.None: break;
                    case TaskType.Interact: if (InteractTask(assignedTasks[i])) { finishedTasks[i] = true; } break;
                    case TaskType.Terminal: if (TerminalTask(assignedTasks[i])) { finishedTasks[i] = true; } break;
                    case TaskType.Pickup: if (PickupTask(assignedTasks[i])) { finishedTasks[i] = true; } break;
                    case TaskType.Paper: break;
                }
            }
            else { numTasksFinished++; }
        }

        tasksCompleted.text = numTasksFinished + "/" + numTasks;
    }

    void AssignTasks()
    {
        taskManager = GameObject.Find("RoomGenerationManager").GetComponent<TaskManager>();
        taskList = taskManager.taskList;
        string goalTextResult = "";

        for (int i = 0; i < numTasks; i++)
        {
            // Add random tasks from total tasks in task manager
            int newTask = Random.Range(0, taskList.Count - 1);
            Debug.Log(newTask);
            //while (assignedTasks.Contains(taskList[newTask])) { newTask = Random.Range(0, taskList.Count - 1); }

            assignedTasks.Add(taskList[newTask]);

            // Add to goalTextResult
            goalTextResult += "Task " + i + ": " + DisplayText(taskList[newTask]) + "\n";
        }

        finishedTasks = new bool[assignedTasks.Count];

        // set initial UI
        goalText.text = goalTextResult;
        tasksCompleted.text = "0/" + numTasks;
    }

    string DisplayText(RoomTask task)
    {
        string triggers = "";
        foreach (GameObject trigger in task.triggerGameObject)
        {
            triggers += " " + trigger.name;
        }

        string triggerRooms = "";
        foreach (Room room in task.rooms1)
        {
            triggerRooms += " " + room.roomName;
        }

        string results = "";
        if (task.resultGameObject != null)
        {
            foreach (GameObject result in task.resultGameObject)
            {
                results += " " + result.name;
            }
        }

        string resultRooms = "";
        foreach (Room room in task.rooms2)
        {
            resultRooms += " " + room.roomName;
        }

        switch (task.type)
        {
            case TaskType.Interact: return "Activate " + results;
            case TaskType.Terminal: return "Use the terminal in " + triggerRooms + " to ____";
            case TaskType.Pickup: return "Move " + triggers + " in " + triggerRooms + " to " + resultRooms;
            case TaskType.Paper: return "Piece the papers in " + triggerRooms + "together in " + resultRooms + ".";
            default: return "";
        }
    }


    bool PickupTask(RoomTask task)
    {
        foreach (GameObject trigger in task.triggerGameObject)
        {
            if (trigger.transform.position != task.position)
            {
                return false;
            }
        }

        return true;
    }

    bool InteractTask(RoomTask task)
    {
        foreach (ObjectData data in task.data1)
        {
            if (!data.GetActiveAnimationState())
            {
                return false;
            }
        }

        //task.data2.SetActiveAnimationState(true);
        return true;
    }

    bool PaperTask(RoomTask task)
    {
        // on completion, display UI to put pieces of paper together
        return true;
    }

    bool TerminalTask(RoomTask task)
    {
        return true; // idk change
    }

}
