using Player;
using System.Collections;
using UnityEngine;

namespace Ship
{
    public class Ladder : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShipPassenger _ShipPassenger;

        [SerializeField] private Transform _TopPosition;
        [SerializeField] private Transform _BottomPosition;

        [SerializeField] private float _PutPlayerOnLadderSpeed = 3f;

        public bool HoldInteraction => false;

        public string InteractionText => "Ladder";

        private IEnumerator _movePlayerCoroutine;

        public void OnInteractionTriggered(PlayerInteraction interact)
        {
            Transform targetPos;

            if (_ShipPassenger.IsOnShip)
            {
                if (_movePlayerCoroutine != null)
                {
                    StopCoroutine(_movePlayerCoroutine);
                    _movePlayerCoroutine = null;
                }
                targetPos = _BottomPosition;
            }

            else
            {
                if (_movePlayerCoroutine != null)
                {
                    StopCoroutine(_movePlayerCoroutine);
                    _movePlayerCoroutine = null;
                }
                targetPos = _TopPosition;
            }

            _movePlayerCoroutine = interact.SlideToPosition(targetPos, _PutPlayerOnLadderSpeed, SlideCoroutineCallback);
            StartCoroutine(_movePlayerCoroutine);
        }

        public void OnRaycastHitEnter()
        {

        }

        public void OnRaycastHitExit()
        {

        }

        private void SlideCoroutineCallback()
        {
            _ShipPassenger.IsOnShip = !_ShipPassenger.IsOnShip;
            _movePlayerCoroutine = null;
        }
    }
}