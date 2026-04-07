using Player;
using System.Collections;
using UnityEngine;

namespace Ship
{
    public class Ladder : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShipPassenger _ShipPassenger;
        [SerializeField] private Transform _Ladder;

        [SerializeField] private float _PutPlayerOnLadderSpeed = 3f;

        public bool HoldInteraction => true;

        public string InteractionText => "Ladder";

        public void OnInteractionTriggered(PlayerInteraction interact)
        {
            _ShipPassenger.IsOnShip = !_ShipPassenger.IsOnShip;
        }

        public void OnRaycastHitEnter()
        {

        }

        public void OnRaycastHitExit() 
        { 

        }

        private void Update()
        {
            
        }
    }
}