using Player;
using System.Collections;
using UnityEngine;

namespace Ship
{
    public class ShipAnchor : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShipMovement _ShipMovement;

        public bool HoldInteraction => false;

        public string InteractionText => "Anchor";

        public void OnInteractionTriggered(PlayerInteraction interact)
        {
            if (_ShipMovement.IsAnchorDown)
                _ShipMovement.SetAnchor(false);
            else 
                _ShipMovement.SetAnchor(true);
        }

        public void OnRaycastHitEnter() { }
        public void OnRaycastHitExit() { }

       
    }
}