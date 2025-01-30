using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Interact : MonoBehaviour
{
    [Header("HitScan Variables")]
    bool canPickup = true; // false if holding something already
    bool isRunning; // checks to see if coroutine is running

    [Header("Highlight Variables")]
    private Color highlightColor = Color.grey;
    private List<Material> materials;
    private GameObject highlightedObject;
    private GameObject pickedupObject;

    [Header("Interactable Variables")]
    [SerializeField] TextMeshProUGUI popupText;
    [SerializeField] GameObject popupMenu;


    bool canChange = true;
    private void Update()
    {
        HitScan();
        if (Input.GetMouseButtonDown(0))
        {
            if (canPickup && highlightedObject != null) { Pickup(); Press(); }
            else { Place(); }
        }

        // Shows the pickedup object in hand
        if (!canPickup)
        {
            pickedupObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.8f + Camera.main.transform.right * 0.5f;
            pickedupObject.transform.rotation = Camera.main.transform.rotation;
        }

    }

    private void Pickup()
    {
        // Picks up highlighted object
        if (highlightedObject.tag == "Selectable")
        {
            canPickup = false;
            pickedupObject = highlightedObject;
            highlightedObject = null;
        }
    }
    private void Place()
    {
        if (pickedupObject != null)
        {
            pickedupObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward + Camera.main.transform.up * 0.3f;
            pickedupObject = null;
            canPickup = true;
        }
    }
    void Press()
    {
        if (highlightedObject != null && highlightedObject.tag == "Button")
        {
            // access object and perform action
            highlightedObject.GetComponent<Animator>().SetTrigger("isPressed");
        }
    }
    void FocusDOS()
    {
        if (highlightedObject != null && highlightedObject.tag == "DOS Terminal")
        {
            // access object and perform action
            highlightedObject.GetComponent<DOSInteraction>().StartInteraction();
        }
    }
    void HitScan()
    {
        // Finds objects that will be hit by raycast
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.5f));
        RaycastHit hit;

        //-- Highlights objects to be picked up -- //
        if (Physics.Raycast(ray, out hit) && (hit.collider.tag == "Selectable" | hit.collider.tag == "Button") && hit.distance < 3)
        {
            Debug.Log("hit scanned something" + hit.collider.gameObject);
            ToggleHighlight(hit.collider.gameObject);

            popupText.text = "Left click";
            if (!isRunning) { StartCoroutine(FadeMenu()); }
        }
        else if (highlightedObject != null && highlightedObject != pickedupObject)
        {
            foreach (var material in materials)
            {
                material.DisableKeyword("_EMISSION");
            }
            highlightedObject = null;
        }

    }

    public void ToggleHighlight(GameObject newObject)
    {
        // Highlights the raycast object
        materials = newObject.GetComponent<Renderer>().materials.ToList();

        foreach (var material in materials)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", highlightColor);
        }

        highlightedObject = newObject; // stores highlighted object.
    }

    IEnumerator FadeMenu()
    {
        Image fader = popupMenu.GetComponent<Image>();
        float transparencyMenu;
        float transparencyText;
        float transition;
        float max = 0.5f;
        float min = 0;
        isRunning = true;

        if (fader.color.a == 0) // fade in
        {
            transparencyMenu = 0;
            transparencyText = 0;
            transition = .01f;
        }
        else // fade out
        {
            transparencyMenu = .48f;
            transparencyText = .96f;
            transition = -.01f;
        }

        // Fades the menu and text out or in
        while (transparencyMenu >= min && transparencyMenu <= max)
        {
            transparencyMenu += transition;
            transparencyText += transition * 2;
            fader.color = new Color(fader.color.r, fader.color.g, fader.color.b, transparencyMenu);
            popupText.color = new Color(255, 0, 0, transparencyText);

            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(5);

        if (transition == .01f) { StartCoroutine(FadeMenu()); }
        else { isRunning = false; fader.color = new Color(fader.color.r, fader.color.g, fader.color.b, 0); }
    }
}
