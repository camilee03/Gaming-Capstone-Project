using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using Unity.Netcode;

public class ClientNetworkAnimator : NetworkAnimator
{
    public bool toggleable;
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AnimToggleServerRpc()
    {
        Debug.Log(gameObject.name + " to be toggled");
        if (toggleable)
        {
            Animator.SetBool("Toggled", !Animator.GetBool("Toggled"));
            Debug.Log("Trying to toggle " + gameObject.name);
        }
        else Debug.Log(gameObject.name + " not toggleable");
    }

}
