using Player;
using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public bool HoldInteraction => true;

    public void OnInteractionTriggered()
    {
        Debug.Log("on interaction triggered");
    }

    public void OnRaycastHitEnter()
    {
        Debug.Log("on raycast hit enter triggered");

    }

    public void OnRaycastHitExit()
    {
        Debug.Log("on raycast hit exit triggered");

    }
}
