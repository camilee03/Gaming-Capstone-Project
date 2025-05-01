using UnityEngine;
using UnityEngine.UI;

public class DoppleAttackTimer : MonoBehaviour
{
    public Slider slider;
    public PlayerController playerController;
    private void Start()
    {
        slider.maxValue = playerController.AttackDelay;
    }
    private void Update()
    {
        if(playerController.isDopple)
        {
            slider.gameObject.SetActive(true);
            slider.value = playerController.attackTimer;
        }
        else
        {
            slider.gameObject.SetActive(false);

        }



    }
}
