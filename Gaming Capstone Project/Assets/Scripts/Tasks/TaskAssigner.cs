using UnityEngine;
using System.Collections.Generic;

public class TaskAssigner : MonoBehaviour
{
    List<RoomTask> taskList;
    List<RoomTask> assignedTasks;
    int numTasks = 3;

    private void Start()
    {
        taskList = GetComponent<TaskManager>().taskList;

        // Add random tasks from total tasks in task manager
        for (int i = 0; i < numTasks; i++)
        {
            int newTask = Random.Range(0, taskList.Count - 1);
            while (assignedTasks.Contains(taskList[newTask])) { newTask = Random.Range(0, taskList.Count - 1); }

            assignedTasks.Add(taskList[newTask]);
        }
    }

    private void Update()
    {
        for (int i=0; i < assignedTasks.Count; i++)
        {
            switch (assignedTasks[i].type)
            {
                case TaskType.None: break;
                case TaskType.Interact: if (InteractTask(assignedTasks[i])) { assignedTasks.RemoveAt(i); } break;
                case TaskType.Terminal: if (TerminalTask(assignedTasks[i])) { assignedTasks.RemoveAt(i); } break;
                case TaskType.Pickup: if (PickupTask(assignedTasks[i])) { assignedTasks.RemoveAt(i); } break;
                case TaskType.Paper: break;
            }
        }
    }


    bool PickupTask(RoomTask task)
    {
        if (task.triggerGameObject.transform.position == task.position)
        {
            return true;
        }

        return false;
    }

    bool InteractTask(RoomTask task)
    {
        if (task.data1.GetActiveAnimationState())
        {
            //task.data2.SetActiveAnimationState(true);
            return true;
        }
        return false;
    }

    bool TerminalTask(RoomTask task)
    {
        return true; // idk change
    }

}
