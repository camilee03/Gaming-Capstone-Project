using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TaskAssigner : MonoBehaviour
{
    // Task containers
    List<RoomTask> taskList;
    List<RoomTask> assignedTasks = new List<RoomTask>();
    Dictionary<RoomTask, bool> finishedTasks;
    int numTasks = 3;

    // Task display
    [SerializeField] TMP_Text tasksCompleted;
    [SerializeField] GameObject notebook;
    [SerializeField] Interact interactManager;
    TaskManager taskManager;

    public bool start = false; // toggle through another function
    bool donow = false;

    // Show task
    RoomTask currentTask;
    int currentTaskIndex;
    [SerializeField] TaskPointer taskPointer;
    [SerializeField] GameObject defaultToggle;
    ToggleGroup toggleGroup;
    List<Toggle> toggles = new List<Toggle>();

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
            CheckCheckboxes();
        }
    }

    void UpdateTasks()
    {
        int numTasksFinished = 0;
        int index = 0;

        foreach (RoomTask task in assignedTasks)
        {
            if (!finishedTasks.Keys.Contains(task) || !finishedTasks[task])
            {
                switch (task.type)
                {
                    case TaskType.None: break;
                    case TaskType.Interact: if (InteractTask(task, index)) { finishedTasks[task] = true; } break;
                    case TaskType.Terminal: if (TerminalTask(task, index)) { finishedTasks[task] = true; } break;
                    case TaskType.Pickup: if (PickupTask(task, index)) { finishedTasks[task] = true; } break;
                    case TaskType.Paper: break;
                }
            }
            else { numTasksFinished++; }
            index++;
        }

        tasksCompleted.text = numTasksFinished + "/" + numTasks;
    }

    void AssignTasks()
    {
        // Should try to assign tasks that aren't the same as anyone else


        taskManager = GameObject.Find("RoomGenerationManager").GetComponent<TaskManager>();
        taskList = taskManager.taskList;
        string goalTextResult = "";

        for (int i = 0; i < numTasks; i++)
        {
            // Add random tasks from total tasks in task manager
            int newTask = Random.Range(0, taskList.Count - 1);
            //while (assignedTasks.Contains(taskList[newTask])) { newTask = Random.Range(0, taskList.Count - 1); }

            assignedTasks.Add(taskList[newTask]);

            // Add new task checkbox
            goalTextResult = "Task " + i + ": " + DisplayText(taskList[newTask]) + "\n";
            CreateCheckboxes(new Vector3(0, 400 - i * 100, 0), i, goalTextResult);
        }

        finishedTasks = new Dictionary<RoomTask, bool>();

        // set initial UI
        tasksCompleted.text = "0/" + numTasks;
    }


    //-- Check if current task is completed --//
    bool PickupTask(RoomTask task, int index)
    {
        foreach (GameObject trigger in task.triggerGameObject)
        {
            if (trigger.transform.position != task.position)
            {
                return false;
            }
        }

        toggles[index].GetComponentInChildren<TMP_Text>().color = Color.green;

        return true;
    }

    bool InteractTask(RoomTask task, int index)
    {
        bool isComplete = true;
        for (int i=0; i < task.data1.Length; i++)
        {
            if (!task.data1[i].GetActiveAnimationState())
            {
                isComplete = false;
            }
            else
            {
                task.data2[i].SetActiveAnimationState(true);
            }
        }

        if (isComplete) { toggles[index].GetComponentInChildren<TMP_Text>().color = Color.green; }

        return isComplete;
    }

    bool PaperTask(RoomTask task, int index)
    {
        // on completion, display UI to put pieces of paper together
        return true;
    }

    bool TerminalTask(RoomTask task, int index)
    {
        bool isComplete = false;
        if (interactManager.highlightedObject == task.triggerGameObject[0]) // If currently interacting with the DOS
        {
            isComplete = true;
            for (int i = 0; i < task.data1.Length; i++)
            {
                // Might need to change for the lights
                if (!task.data1[i].GetActiveAnimationState()) { isComplete = false; }
            }
        }


        if (isComplete) { toggles[index].GetComponentInChildren<TMP_Text>().color = Color.green; }
        return isComplete; 
    }


    // -- UI --//
    string DisplayText(RoomTask task)
    {
        string triggers = "";
        foreach (GameObject trigger in task.triggerGameObject)
        {
            triggers += " " + trigger.name.Replace("(Clone)", "");
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
                results += " " + result.name.Replace("(Clone)", "");
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

    public void OpenTaskMenu(InputAction.CallbackContext context)
    {
        notebook.SetActive(!notebook.activeSelf);
    }

    void ShowCurrentTask()
    {
        // If player is holding a task item
        if (interactManager.pickedupObject != null && currentTask.triggerGameObject.Contains(interactManager.pickedupObject))
        {
            // set location to where that task item needs to be placed
            taskPointer.SetTarget(currentTask.position);
        }

        else
        {
            foreach (GameObject trigger in currentTask.triggerGameObject)
            {
                // set location to first trigger object if not already finished (check condition based on task?)
                taskPointer.SetTarget(trigger.transform.position);
                break;
            }
        }
    }

    void CreateCheckboxes(Vector3 position, int number, string description)
    {
        // Instantiate toggle
        GameObject newToggleObject = GameObject.Instantiate(defaultToggle);
        newToggleObject.name = number.ToString();
        newToggleObject.transform.parent = notebook.transform;
        newToggleObject.transform.localPosition = position;

        // Get & set components
        Toggle newToggle = newToggleObject.GetComponent<Toggle>();
        newToggleObject.GetComponentInChildren<Text>().text = description;
        toggles.Add(newToggle);

        // Set toggle to a group
        if (toggleGroup == null) { toggleGroup = notebook.AddComponent<ToggleGroup>(); toggleGroup.allowSwitchOff = true; }
        newToggle.group = toggleGroup;
    }

    void CheckCheckboxes()
    {
        if (toggleGroup.AnyTogglesOn())
        {
            currentTask = assignedTasks[int.Parse(toggleGroup.GetFirstActiveToggle().name)];
            ShowCurrentTask();

            // something to move the toggle if task already completed?
        }
    }
}
