using Player;
using UnityEngine;

namespace Ship
{
    public class ShipMovement : MonoBehaviour
    {
        [SerializeField] private float _MaxSpeed = 9f;
        [SerializeField] private float _AccelRatio = .007f;
        [SerializeField] private float _DeccelRatio = .025f;
        [SerializeField] private float _RotationSpeed = 20f;

        [Header("Collision")]
        [SerializeField] private float _CollisionCheckRadius = 3f;
        [SerializeField] private float _CollisionCheckDistance = 5f;

        private LayerMask _islandLayerMask;

        private float _currentSpeed = 0;
        private float _aimedSpeed = 0;
        private float _wheelDirection = 0;
        private bool _isAnchorDown = false;

        public bool IsAnchorDown => _isAnchorDown;

        private void Start()
        {
            _islandLayerMask = LayerMask.GetMask("Island");
        }

        private void Update()
        {
            if (!_isAnchorDown)
            {
                _aimedSpeed = _MaxSpeed;
                _currentSpeed = Mathf.Lerp(_currentSpeed, _aimedSpeed, _AccelRatio);
            }
            else
            {
                _aimedSpeed = 0;
                _currentSpeed = Mathf.Lerp(_currentSpeed, _aimedSpeed, _DeccelRatio);
            }

            if (_wheelDirection != 0)
            {
                transform.Rotate(0, _wheelDirection * _RotationSpeed * Time.deltaTime, 0);
            }

            RaycastHit hit;

            Vector3 castOrigin = new Vector3(transform.position.x, 0f, transform.position.z);
            if (Physics.SphereCast(castOrigin, _CollisionCheckRadius, transform.forward, out hit, _CollisionCheckDistance, _islandLayerMask))
            {
                _currentSpeed = 0;
            }

            transform.position += transform.forward * _currentSpeed * Time.deltaTime;
        }

        public void SetRudder(float direction)
        {
            _wheelDirection = direction;
        }

        public void SetAnchor (bool dropped)
        {
            _isAnchorDown = dropped;
        }
    }
}

