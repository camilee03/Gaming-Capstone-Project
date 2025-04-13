using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Interact : NetworkBehaviour
{
    [Header("HitScan Variables")]
    bool canPickup = true; // false if holding something already

    [Header("Highlight Variables")]
    private Color highlightColor = new Color(0.01f, 0.02f, 0f, 1f);
    private List<Material> materials;
    public GameObject highlightedObject;
    public GameObject pickedupObject;
    private Rigidbody pickedupRigidBody;

    [Header("Interactable Variables")]
    string[] tags = { "Selectable", "Button", "Door", "DOS Terminal" };

    PlayerController player;
    public Transform rightHand;
    public Animator anim;
    public Vector3 offset;

    bool canChange = true;
    private void Start()
    {
        player = gameObject.GetComponentInParent<PlayerController>();
    }

    private void FixedUpdate()
    {
        HitScan();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) { OnClick(); }

        // Shows the pickedup object in hand
        if (!canPickup && pickedupObject != null)
        {
            pickedupObject.transform.position = rightHand.position + (rightHand.forward * offset.x) + (rightHand.up * offset.y) + (rightHand.right * offset.z);
            pickedupObject.transform.rotation = rightHand.rotation;
        }
        
        //Check for Doppel Transform, must drop if transformed.
        if (anim.GetBool("Transformed"))
        {
            Place();
            canPickup = false;
        }
        else if (pickedupObject == null) //reenable picking up if untransformed.
        {
            canPickup = true;
        }

    }

    public void OnClick()
    {
        if (highlightedObject != null)
        {
            switch (highlightedObject.tag)
            {
                case "Selectable": // Pick up object
                    if (canPickup)
                    {
                        canPickup = false;
                        pickedupObject = highlightedObject;
                        if (pickedupObject.GetComponent<Rigidbody>())
                        {
                            pickedupRigidBody = pickedupObject.GetComponent<Rigidbody>();
                            pickedupRigidBody.useGravity = false;
                        }
                        Collider[] colliders = pickedupObject.GetComponents<Collider>();
                        foreach (Collider collider in colliders)
                        {
                            collider.enabled = false;
                        }
                        anim.SetLayerWeight(1, 1);
                        highlightedObject = null;
                    }
                    break;
                case "Button": // Press button / lever
                    toggleAnimatedObject(highlightedObject);
                    break;
                case "Door": // Open / close door
                    toggleAnimatedObject(highlightedObject);
                    break;
                case "DOS Terminal": // access object and perform action
                    highlightedObject.GetComponent<DOSInteraction>().SetInteract(gameObject.GetComponent<Interact>());
                    highlightedObject.GetComponent<DOSInteraction>().ToggleInteraction();
                    highlightedObject.GetComponent<DOSInteraction>().SetCam(gameObject);
                    break;
            }
        }
        else { Place(); }
    }

    private void Place()
    {
        if (pickedupObject != null)
        {
            pickedupObject.transform.position = rightHand.position + rightHand.forward * offset.x + rightHand.up * offset.y + rightHand.right * offset.z;

            Collider[] colliders = pickedupObject.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = true;
            }
            anim.SetLayerWeight(1, 0);
            if (pickedupRigidBody != null)
            {
                pickedupRigidBody.useGravity = true;
            }
            pickedupObject = null;
            pickedupRigidBody = null;
            canPickup = true;
        }
    }

    void HitScan()
    {
        // Finds objects that will be hit by raycast
        LayerMask layerMask = LayerMask.GetMask("Player");
        RaycastHit hit;

        //-- Highlights objects to be picked up -- //
        if (Physics.Raycast(transform.position + transform.forward * 2, transform.forward, out hit, 10, ~layerMask) && tags.Contains(hit.collider.tag))
        {
            Debug.Log("hit scanned something" + hit.collider.gameObject);
            if (highlightedObject == null) { EnableHighlight(hit.collider.gameObject); }
        }
        else if (highlightedObject != null && highlightedObject != pickedupObject)
        {
            DisableHighlight();
        }

    }

    public void EnableHighlight(GameObject newObject)
    {
        Debug.Log("Enabling highlight on " + newObject.name);
        // Highlights the raycast object
        materials = newObject.GetComponent<Renderer>().materials.ToList();

        foreach (var material in materials)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", highlightColor);
        }

        highlightedObject = newObject; // stores highlighted object.
    }

    public void DisableHighlight()
    {
        Debug.Log("Disabling highlight on " + highlightedObject.name);
        foreach (var material in materials)
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
        highlightedObject = null;
    }

    public void toggleAnimatedObject(GameObject g)
    {
        g.GetComponent<ClientNetworkAnimator>().AnimToggleServerRpc();
        Debug.Log("Toggled " + g.name);
    }

}
