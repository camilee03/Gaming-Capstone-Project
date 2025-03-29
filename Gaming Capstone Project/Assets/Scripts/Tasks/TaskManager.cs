using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using static TaskManager;

public class TaskManager : MonoBehaviour
{
    [Header("UI")]
    TMP_Text taskCommand;

    public List<RoomTask> taskList;


    TaskType task;
    int numTasks;

    private void Start()
    {
        taskList = new List<RoomTask>();

        GameObject[] selectables = GameObject.FindGameObjectsWithTag("Selectable");
        GameObject[] buttons = GameObject.FindGameObjectsWithTag("Button");
        GameObject[] terminals = GameObject.FindGameObjectsWithTag("DOS Terminal");
        GameObject[] useables = GameObject.FindGameObjectsWithTag("Useable");
        //GameObject[] papers = GameObject.FindGameObjectsWithTag("Paper");

        if (useables.Length > buttons.Length)
        {
            // Create at least one double action lever/button
            int numDoubles = useables.Length - buttons.Length;
            int ii = 0;
            for (int i=0; i<buttons.Length-1; i++)
            {
                if (i >= buttons.Length - Mathf.Ceil(numDoubles/2.0f))
                {
                    taskList.Add(CreateTask(TaskType.Interact, buttons[i], new GameObject[2] { useables[ii], useables[ii+1] }));
                    ii += 2;
                }
                else
                {
                    taskList.Add(CreateTask(TaskType.Interact, buttons[i], new GameObject[1] { useables[i] }));
                    ii = i + 1;
                }
            }

        }
        else if (buttons.Length > useables.Length)
        {
            // Create at least one multi lever/button task (think puzzle)

        }
        else
        {
            // One button per use
        }
    }


    private RoomTask CreateTask(TaskType taskType, GameObject gameObject1, GameObject[] gameObject2)
    {
        // Find position if needed
        Vector3 position = Vector3.zero;
        if (taskType == TaskType.Pickup || taskType == TaskType.Paper)
        {
            GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
            int randomTilePos = Random.Range(0, tiles.Length - 1);
            position = tiles[randomTilePos].transform.position;
        }

        // Find data of objects list
        ObjectData[] objectData = null;
        if (gameObject2 != null)
        {
            objectData = new ObjectData[gameObject2.Length];

            for (int i=0; i<gameObject2.Length-1; i++)
            {
                objectData[i] = gameObject2[i].GetComponent<ObjectData>();
            }
        }

        // Create task 
        RoomTask task = new RoomTask() { 
            type = taskType,
            triggerGameObject = gameObject1,
            data1 = gameObject1.GetComponent<ObjectData>(),
            resultGameObject = gameObject2,
            data2 = objectData,
            position = position,
        };

        return task;
    }
}
public struct RoomTask
{
    public TaskType type;
    public GameObject triggerGameObject;
    public ObjectData data1;
    public GameObject[] resultGameObject;
    public ObjectData[] data2;
    public Vector3 position;
}
public enum TaskType
{
    None, Pickup, Interact, Terminal, Paper
}