using Player;
using System.Collections;
using UnityEngine;

namespace Ship
{
    public class ShipWheel : MonoBehaviour, IInteractable
    {
        [SerializeField] private ShipMovement _ShipMovement;
        [SerializeField] private Transform _PlayerWheelPosition;

        [SerializeField] private float _PutPlayerOnWheelSpeed = 3f;

        private PlayerInteraction _playerInteraction;
        private bool _isPlayerSteering = false;
        private bool _isPlayerInPosition = false;
        private IEnumerator _movePlayerCoroutine;

        public bool HoldInteraction => true;

        public string InteractionText => "Wheel";

        public void OnInteractionTriggered(PlayerInteraction interact)
        {
            if (_playerInteraction == null)
                _playerInteraction = interact;

            if (_isPlayerSteering)
            {
                if (_movePlayerCoroutine != null)
                    StopCoroutine(_movePlayerCoroutine);

                _movePlayerCoroutine = null;

                _isPlayerSteering = false;
                _isPlayerInPosition = false;

                _playerInteraction.PlayerMotor.SetMovementEnabled(true);
                _playerInteraction.PlayerMotor.SetJumpEnabled(true);
                _playerInteraction.PlayerMotor.SetHeadBobEnabled(true);
                _playerInteraction.PlayerMotor.GetCharacterController().enabled = true;

                _playerInteraction.ShipPassenger.enabled = true;
            }
            else
            {
                _isPlayerSteering = true;

                _playerInteraction.PlayerMotor.SetMovementEnabled(false);
                _playerInteraction.PlayerMotor.SetJumpEnabled(false);
                _playerInteraction.PlayerMotor.SetHeadBobEnabled(false);

                _movePlayerCoroutine = _playerInteraction.SlideToPosition(_PlayerWheelPosition, _PutPlayerOnWheelSpeed, SlidePlayerCoroutineDone);
                StartCoroutine(_movePlayerCoroutine);
            }
        }

        public void OnRaycastHitEnter()
        {

        }

        public void OnRaycastHitExit() 
        { 

        }

        private void Update()
        {
            if (_isPlayerSteering && _isPlayerInPosition)
                _ShipMovement.SetRudder(_playerInteraction.MoveInput.x);

            else if (!_isPlayerSteering)
                _ShipMovement.SetRudder(0);
        }

        private void SlidePlayerCoroutineDone()
        {
            _isPlayerInPosition = true;
        }
    }
}