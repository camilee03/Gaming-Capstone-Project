using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;



// MENTAL NOTE: MAKE SURE TASK POSITION SPAWNS > 50 AWAY FROM THE CURRENT OBJECT

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
<<<<<<< Updated upstream
    [SerializeField] GameObject taskLayoutGroup;
=======
    [SerializeField] GameObject TaskCanvas;
>>>>>>> Stashed changes
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
            if (playerController.isDopple) { tasksCompleted.text = "You have no tasks, you are a dopple."; }
            else
            {
                AssignTasks();
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
                }
            }
            else { numTasksFinished++; }
            index++;
        }

        tasksCompleted.text = numTasksFinished + "/" + numTasks;

        if (numTasksFinished == numTasks) { taskManager.UpdateTasks(); numTasks = -1; }
    }

    void AssignTasks()
    {
        taskManager = GameObject.Find("RoomGenerationManager").GetComponent<TaskManager>();
        taskList = taskManager.taskList;
        string goalTextResult = "";

        for (int i = 0; i < numTasks; i++)
        {
            if (taskList.Count == 0) { break; }

            // Add random tasks from total tasks in task manager
            int newTask = Random.Range(0, taskList.Count - 1);

            assignedTasks.Add(taskList[newTask]);
            taskList.Remove(taskList[newTask]); // make sure other players can't get this task

            // Add new task checkbox
            goalTextResult = "Task " + i + ": " + DisplayText(taskList[newTask]) + "\n";
            CreateCheckboxes(new Vector3(200 + (i * 100), -200, 0), i, goalTextResult);
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
            if (Vector3.Distance(trigger.transform.position, task.position) > 30)
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
<<<<<<< Updated upstream
        notebook.SetActive(!notebook.activeSelf);
        anim.SetLayerWeight(3, 1 - anim.GetLayerWeight(3));

        if (notebook.activeSelf) { Cursor.lockState = CursorLockMode.None; }
        else { Cursor.lockState = CursorLockMode.Locked; }
=======
        anim.SetLayerWeight(3, 1 - anim.GetLayerWeight(3));
        //this.transform.GetChild(0).gameObject.SetActive(!notebook.activeSelf);
        //notebook.SetActive(!notebook.activeSelf);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        newToggleObject.transform.SetParent(taskLayoutGroup.transform); // gets canvas

        RectTransform toggleTransform = newToggleObject.GetComponent<RectTransform>();
        toggleTransform.localRotation = Quaternion.identity;
        toggleTransform.localPosition = Vector3.zero;

        toggleTransform.localScale = new Vector3(6, 2, 2);
=======
        newToggleObject.transform.parent = TaskCanvas.transform; // gets canvas
        newToggleObject.GetComponent<RectTransform>().localRotation = Quaternion.identity;
        newToggleObject.GetComponent<RectTransform>().localPosition = Vector3.zero;
        newToggleObject.GetComponent<RectTransform>().localScale = new Vector3(6, 2, 2);
>>>>>>> Stashed changes

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
