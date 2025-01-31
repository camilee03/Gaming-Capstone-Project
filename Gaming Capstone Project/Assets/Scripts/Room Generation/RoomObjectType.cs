using System.Xml.Serialization;
using UnityEngine;

public class RoomObjectType : MonoBehaviour
{
    enum ItemType { Box, Button, Lever, Light, Table, Chair, 
        Doors, BulletinBoard, Radio, Terminal, Fan, Wires, 
        Furnace, Coal, Vent, Food, WashingMachine, Clothes, Cabinet, 
        EnergyCore, Trash }

    enum TaskCategory { Pickup, Interactable, Attack }

    enum TaskType { Visual, Audio, Tactile, None }

    enum Constraints { None, Orientation, Wall, Ceiling, }

    private void SpawnObject(Constraints con, char tileCode) // a generic function for spawning objects randomly
    {

    }

    private void DecorateRoom() // fills a room with appropriate objects
    {
        //-- Doors (d) --//
        int n = 4; // number of walls
        int num = Random.Range(1, n); // constraints: one door per wall
        for (int i = 0; i < num; i++) { SpawnObject(Constraints.Wall, 'd'); }

        //-- Board (b) --//
        SpawnObject(Constraints.Wall, 'b');

        //-- DOS (t) --//
        SpawnObject(Constraints.None, 't');

        //-- Lights (l) --//
        n = 4; // number of floors
        num = Random.Range(1, n);
        for (int i = 0; i < num; i++) { SpawnObject(Constraints.Ceiling, 'l'); }

        //-- Fan (f) --//

        //-- Other (o) --//
        
    }

    private void DetermineTheme() // determine task type and theme based on GPT
    {

    }

    private void DetermineTask() // determine tasks based on GPT model
    { 

    }

    private void AddScripts() // add scripts to objects for interaction and connection when needed
    { 

    }
}
