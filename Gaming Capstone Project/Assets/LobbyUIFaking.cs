using Unity.Netcode;
using UnityEngine;

public class LobbyUIFaking : NetworkBehaviour
{
    private GameController gameController;
    private int numDopples = 1;

    private void Start()
    {
        gameController = GameController.Instance;
    }

    private void Update()
    {
        // Press COMMA to increase dopples
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            Debug.Log("Increasing Dopples");
            numDopples++;
            gameController.SetNumberOfDopples(numDopples);
        }

        // Press PERIOD to decrease dopples
        if (Input.GetKeyDown(KeyCode.Period))
        {
            Debug.Log("Decreasing Dopples");
            if (numDopples > 1)
            {
                numDopples--;
                gameController.SetNumberOfDopples(numDopples);
            }
        }


    }
}
