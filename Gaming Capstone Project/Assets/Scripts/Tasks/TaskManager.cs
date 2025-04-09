using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using static TaskManager;

public class TaskManager : MonoBehaviour
{
    [Header("UI")]
    TMP_Text taskCommand;

    List<Room> rooms;

    [Header("Task Holders")]
    TaskType task;
    int numTasks;
    public List<RoomTask> taskList;

    [Header("Paper Task")]
    TextAsset[] documents;

    [Header("Types of Tasks")]

    GameObject[] selectables;
    GameObject[] buttons;
    GameObject[] terminals;
    GameObject[] useables;

    public void CreateTasks()
    {
        selectables = GameObject.FindGameObjectsWithTag("Selectable");
        buttons = GameObject.FindGameObjectsWithTag("Button");
        terminals = GameObject.FindGameObjectsWithTag("DOS Terminal");
        useables = GameObject.FindGameObjectsWithTag("Useable");
        //papers = GameObject.FindGameObjectsWithTag("Paper");

        taskList = new List<RoomTask>();
        rooms = RoomManager.Instance.rooms;

        CreateInteractTasks(useables, buttons);
        CreatePickupTasks(selectables);
        CreateTerminalTasks();
        CreatePaperTasks();
        
        Debug.Log("NUM TASKS: " + taskList.Count);
    }

    void CreateInteractTasks(GameObject[] useables, GameObject[] buttons)
    {
        if (useables.Length > buttons.Length)
        {
            // Create at least one double action lever/button
            int numDoubles = useables.Length - buttons.Length;
            int ii = 0;
            for (int i = 0; i < buttons.Length - 1; i++)
            {
                if (i >= buttons.Length - Mathf.Ceil(numDoubles / 2.0f))
                {
                    taskList.Add(CreateTask(TaskType.Interact, new GameObject[1] { buttons[i] }, new GameObject[2] { useables[ii], useables[ii + 1] }));
                    ii += 2;
                }
                else
                {
                    taskList.Add(CreateTask(TaskType.Interact, new GameObject[1] { buttons[i] }, new GameObject[1] { useables[i] }));
                    ii = i + 1;
                }
            }

        }
        else if (buttons.Length > useables.Length)
        {
            // Create at least one multi lever/button task (think puzzle)
            int numDoubles = buttons.Length - useables.Length;
            int ii = 0;
            for (int i = 0; i < useables.Length - 1; i++)
            {
                if (i >= useables.Length - Mathf.Ceil(numDoubles / 2.0f))
                {
                    taskList.Add(CreateTask(TaskType.Interact, new GameObject[2] { buttons[ii], buttons[ii + 1] }, new GameObject[1] { useables[i] }));
                    ii += 2;
                }
                else
                {
                    taskList.Add(CreateTask(TaskType.Interact, new GameObject[2] { buttons[ii], buttons[ii + 1] }, new GameObject[1] { useables[i] }));
                    ii = i + 1;
                }
            }
        }
        else
        {
            // One button per use
            for (int i = 0; i < useables.Length - 1; i++)
            {
                taskList.Add(CreateTask(TaskType.Interact, new GameObject[1] { buttons[i] }, new GameObject[1] { useables[i] }));
            }
        }

    }

    void CreatePickupTasks(GameObject[] selectables)
    {
        foreach (GameObject obj in selectables)
        {
            Debug.Log("HERE");
            taskList.Add(CreateTask(TaskType.Pickup, new GameObject[1] { obj }, null));
        }
    }

    void CreateTerminalTasks()
    {
        // Create a series of terminal tasks for each room
        foreach (Room room in rooms)
        {
            // set terminal as triggergameobject

            // Create light task

            // Create fan task if there is a fan in the room

            // Others
        }
    }

    void CreatePaperTasks()
    {

    }

    private RoomTask CreateTask(TaskType taskType, GameObject[] gameObject1, GameObject[] gameObject2)
    {
        // Find position and room if needed
        Vector3 position = Vector3.zero;
        List<Room> rooms1 = new List<Room>();
        List<Room> rooms2 = new List<Room>();
        if (taskType == TaskType.Pickup || taskType == TaskType.Paper)
        {
            int randomRoom = Random.Range(0, rooms.Count - 1);
            rooms2.Add(rooms[randomRoom]);

            int randomTile = Random.Range(0, rooms[randomRoom].tileParent.transform.childCount - 1);
            position = rooms[randomRoom].tileParent.transform.GetChild(randomTile).position;
        }

        // Find data & location of objects 1 list
        ObjectData[] objectData1 = null;
        if (gameObject1 != null)
        {
            objectData1 = new ObjectData[gameObject1.Length];

            for (int i = 0; i < gameObject1.Length - 1; i++)
            {
                objectData1[i] = gameObject1[i].GetComponent<ObjectData>();
                rooms1.Add(gameObject1[i].transform.parent.parent.GetComponent<Room>());
            }
        }

        // Find data & location of objects 2 list
        ObjectData[] objectData2 = null;
        if (gameObject2 != null)
        {
            objectData2 = new ObjectData[gameObject2.Length];

            for (int i=0; i<gameObject2.Length-1; i++)
            {
                objectData2[i] = gameObject2[i].GetComponent<ObjectData>();
                rooms2.Add(gameObject2[i].transform.parent.parent.GetComponent<Room>());
            }
        }

        // Create task 
        RoomTask task = new RoomTask() { 
            type = taskType,
            triggerGameObject = gameObject1,
            data1 = objectData1,
            rooms1 = rooms1,
            resultGameObject = gameObject2,
            data2 = objectData2,
            rooms2 = rooms2,
            position = position,
        };

        return task;
    }
}
public struct RoomTask
{
    public TaskType type;
    public GameObject[] triggerGameObject;
    public ObjectData[] data1;
    public List<Room> rooms1;
    public GameObject[] resultGameObject;
    public ObjectData[] data2;
    public List<Room> rooms2;
    public Vector3 position;
}

public enum TaskType
{
    None, Pickup, Interact, Terminal, Paper
}