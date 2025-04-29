using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.EventSystems;



// MENTAL NOTE: MAKE SURE TASK POSITION SPAWNS > 50 AWAY FROM THE CURRENT OBJECT

public class TaskAssigner : NetworkBehaviour
{
    // Task containers
    List<RoomTask> taskList;
    List<RoomTask> assignedTasks = new List<RoomTask>();
    Dictionary<RoomTask, bool> finishedTasks;
    int numTasks = 3;

    // Task display
    [SerializeField] TMP_Text tasksCompleted;
    [SerializeField] GameObject notebook;
    [SerializeField] GameObject taskLayoutGroup;
    [SerializeField] Interact interactManager;
    PlayerController playerController;
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
    public Animator anim;

    const string STRIKE_START = "<s>";
    const string STRIKE_END = "</s>";

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
            if (start)
            {
                if (false && playerController.isDopple) { tasksCompleted.text = "You have no tasks, you are a dopple."; }
                else
                {
                    AssignTasksClientRpc();
                    start = false;
                    donow = true;
                }
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
                    case TaskType.EnergyCore: if (EnergyCoreTask(task, index)) { finishedTasks[task] = true; } break;
                }
            }
            else { numTasksFinished++; }

            index++;
        }

        tasksCompleted.text = numTasksFinished + "/" + numTasks;

        if (numTasksFinished == numTasks) { taskManager.UpdateTasks(); numTasks = -1; }

        // Change to different task if current task is complete
        else if (finishedTasks.ContainsKey(currentTask) && finishedTasks[currentTask]) 
        {
            index = 0;
            foreach (RoomTask task in assignedTasks)
            {
                if (finishedTasks.ContainsKey(task) && !finishedTasks[task]) { toggles[index].isOn = true; currentTask = task; ShowCurrentTask(); break; }
                index++;
            }
        }
    }

    [ClientRpc]
    void AssignTasksClientRpc()
    {
        if (IsOwner)
        {
            taskManager = GameObject.Find("RoomGenerationManager").GetComponent<TaskManager>();
            taskList = taskManager.taskList;
            string goalTextResult = "";
            if(taskList == null)
            {
                Debug.Log("task list null");
            }
            for (int i = 0; i < numTasks; i++)
            {
                if (taskList.Count == 0) { break; }

                // Add random tasks from total tasks in task manager
                int newTask = Random.Range(0, taskList.Count);

                assignedTasks.Add(taskList[newTask]);

                // Add new task checkbox
                goalTextResult = "Task " + i + ": " + DisplayText(taskList[newTask]) + "\n";
                CreateCheckboxes(new Vector3(-200, 400 - i * 100, 0), i, goalTextResult);

                taskList.Remove(taskList[newTask]); // make sure other players can't get this task
            }

            finishedTasks = new Dictionary<RoomTask, bool>();

            // set initial UI
            tasksCompleted.text = "0/" + numTasks;
        }
    }


    //-- Check if current task is completed --//
    bool PickupTask(RoomTask task, int index)
    {
        foreach (GameObject trigger in task.triggerGameObject)
        {
            if (Vector3.Distance(trigger.transform.position, task.position) > 5)
            {
                return false;
            }
        }

        TMP_Text toggleText = toggles[index].GetComponentInChildren<TMP_Text>();
        toggleText.richText = true;
        if (!toggleText.text.StartsWith("<")) { toggleText.text = STRIKE_START + toggleText.text + STRIKE_END; }

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

        TMP_Text toggleText = toggles[index].GetComponentInChildren<TMP_Text>();
        toggleText.richText = true;
        if (!toggleText.text.StartsWith("<")) { toggleText.text = STRIKE_START + toggleText.text + STRIKE_END; }

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

        TMP_Text toggleText = toggles[index].GetComponentInChildren<TMP_Text>();
        toggleText.richText = true;
        if (!toggleText.text.StartsWith("<")) { toggleText.text = STRIKE_START + toggleText.text + STRIKE_END; }

        return isComplete; 
    }

    bool EnergyCoreTask(RoomTask task, int index)
    {
        bool completed = task.core.taskIsDone;

        TMP_Text toggleText = toggles[index].GetComponentInChildren<TMP_Text>();
        toggleText.richText = true;
        if (!toggleText.text.StartsWith("<") && completed) { toggleText.text = STRIKE_START + toggleText.text + STRIKE_END; }

        return completed;
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
            case TaskType.Interact: return "Activate" + results;
            case TaskType.EnergyCore: return "Activate the energy core in " + triggerRooms;
            case TaskType.Terminal: return "Use the terminal in" + triggerRooms + " to ____";
            case TaskType.Pickup: return "Move" + triggers + " in" + triggerRooms + " to" + resultRooms;
            case TaskType.Paper: return "Piece the papers in " + triggerRooms + " together in" + resultRooms + ".";
            default: return "";
        }
    }

    public void OpenTaskMenu(InputAction.CallbackContext context)
    {
        anim.SetLayerWeight(3, 1 - anim.GetLayerWeight(3));

        if (notebook.activeSelf) { Cursor.lockState = CursorLockMode.Locked; }
        else { Cursor.lockState = CursorLockMode.None; }
        taskLayoutGroup.SetActive(notebook.activeSelf);
    }

    void ShowCurrentTask()
    {
        // If player is holding a task item
        if (interactManager.pickedupObject != null && currentTask.triggerGameObject.Contains(interactManager.pickedupObject))
        {
            // set location to where that task item needs to be placed
            taskPointer.gameObject.SetActive(true);
            taskPointer.SetTarget(currentTask.position);
        }

        else
        {
            foreach (GameObject trigger in currentTask.triggerGameObject)
            {
                // set location to first trigger object if not already finished (check condition based on task?)
                taskPointer.gameObject.SetActive(true);
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
        newToggleObject.transform.SetParent(taskLayoutGroup.transform); // gets canvas

        RectTransform toggleTransform = newToggleObject.GetComponent<RectTransform>();
        toggleTransform.localRotation = Quaternion.identity;
        toggleTransform.localPosition = Vector3.zero;

        toggleTransform.localScale = new Vector3(6, 2, 2);

        // Get & set components
        Toggle newToggle = newToggleObject.GetComponent<Toggle>();
        newToggleObject.GetComponentInChildren<TMP_Text>().text = description;
        toggles.Add(newToggle);

        // Set toggle to a group
        if (toggleGroup == null) { toggleGroup = notebook.AddComponent<ToggleGroup>(); toggleGroup.allowSwitchOff = true; }
        newToggle.group = toggleGroup;

        //EventSystem.current.firstSelectedGameObject = newToggleObject;
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
