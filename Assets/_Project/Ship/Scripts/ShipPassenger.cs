using FPSController;
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

        private void OnEnable()
        {
            _Motor.SetGravityEnabled(false);
        }

        private void OnDisable()
        {
            _Motor.SetGravityEnabled(true);
        }

        private void Start()
        {
            _previousShipPosition = _Ship.position;
            _previousShipRotation = _Ship.rotation;
        }

        private void Update()
        {
            Vector3 shipDelta = _Ship.position - _previousShipPosition;
            Quaternion deltaRotation = _Ship.rotation * Quaternion.Inverse(_previousShipRotation);
            Vector3 offsetFromShip = transform.position - _Ship.position;
            Vector3 rotatedOffset = deltaRotation * offsetFromShip;
            Vector3 rotationDelta = rotatedOffset - offsetFromShip;

            _CharacterController.Move(shipDelta + rotationDelta);
            transform.rotation = deltaRotation * transform.rotation;

            _previousShipPosition = _Ship.position;
            _previousShipRotation = _Ship.rotation;
        }
    }
}