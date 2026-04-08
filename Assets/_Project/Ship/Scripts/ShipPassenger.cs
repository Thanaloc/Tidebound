using FPSController;
using System;
using UnityEngine;

namespace Ship
{
    /// <summary>
    /// Keeps the CharacterController glued to the ship by applying
    /// the ship's frame-to-frame movement delta every frame.
    /// </summary>
    public class ShipPassenger : MonoBehaviour
    {
        [SerializeField] private Transform _Ship;
        [SerializeField] private CharacterController _CharacterController;
        [SerializeField] private PlayerMotor _Motor;

        private Vector3 _previousShipPosition;
        private Quaternion _previousShipRotation;

        public Action<bool> OnPlayerOnShipChanged;

        private bool _isOnShip;

        public bool IsOnShip
        {
            get => _isOnShip;
            set
            {
                _isOnShip = value;
                _Motor.SetGravityEnabled(!_isOnShip);

                if (_isOnShip)
                    _CharacterController.transform.SetParent(_Ship);
                else
                    _CharacterController.transform.SetParent(null);

                OnPlayerOnShipChanged?.Invoke(_isOnShip);
            }
        }

        private void Start()
        {
            _previousShipPosition = _Ship.position;
            _previousShipRotation = _Ship.rotation;
        }

        private void Update()
        {
            Vector3 currentPos = _Ship.position;
            Quaternion currentRot = _Ship.rotation;

            if (!_isOnShip || !_CharacterController.enabled)
            {
                _previousShipPosition = currentPos;
                _previousShipRotation = currentRot;
                return;
            }

            Vector3 shipDelta = currentPos - _previousShipPosition;
            Quaternion deltaRotation = _Ship.rotation * Quaternion.Inverse(_previousShipRotation);
            Vector3 offsetFromShip = transform.position - _Ship.position;
            Vector3 rotatedOffset = deltaRotation * offsetFromShip;
            Vector3 rotationDelta = rotatedOffset - offsetFromShip;

            _CharacterController.Move(shipDelta + rotationDelta);
            transform.rotation = deltaRotation * transform.rotation;

            _previousShipPosition = currentPos;
            _previousShipRotation = currentRot;
        }
    }
}