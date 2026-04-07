using UnityEngine;

namespace Ship
{
    public class ShipMovement : MonoBehaviour
    {
        [SerializeField] private float _MaxSpeed = 9f;
        [SerializeField] private float _Acceleration = 2f;
        [SerializeField] private float _Deceleration = 5f;
        [SerializeField] private float _RotationSpeed = 20f;

        [Header("Collision")]
        [SerializeField] private float _CollisionCheckRadius = 3f;
        [SerializeField] private float _CollisionCheckDistance = 5f;
        [SerializeField] private float _PushBackForce = 2f;

        private LayerMask _islandLayerMask;

        private float _currentSpeed = 0;
        private float _wheelDirection = 0;
        private bool _isAnchorDown = true;

        public bool IsAnchorDown => _isAnchorDown;

        private void Start()
        {
            _islandLayerMask = LayerMask.GetMask("Island");
        }

        private void Update()
        {
            if (!_isAnchorDown)
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, _MaxSpeed, _Acceleration * Time.deltaTime);
            else
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _Deceleration * Time.deltaTime);

            if (_wheelDirection != 0)
                transform.Rotate(0, _wheelDirection * _RotationSpeed * Time.deltaTime, 0);

            Vector3 castOrigin = new Vector3(transform.position.x, 0f, transform.position.z);

            Collider[] overlaps = Physics.OverlapSphere(castOrigin, _CollisionCheckRadius, _islandLayerMask);
            if (overlaps.Length > 0)
            {
                Vector3 closest = overlaps[0].ClosestPoint(castOrigin);
                Vector3 away = new Vector3(castOrigin.x - closest.x, 0f, castOrigin.z - closest.z).normalized;
                transform.position += away * _PushBackForce * Time.deltaTime;
                _currentSpeed = 0;
            }

            RaycastHit hit;
            if (Physics.SphereCast(castOrigin, _CollisionCheckRadius, transform.forward, out hit, _CollisionCheckDistance, _islandLayerMask))
            {
                _currentSpeed = 0;
                Vector3 pushBack = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                transform.position += pushBack * _PushBackForce * 0.5f * Time.deltaTime;
            }

            transform.position += transform.forward * _currentSpeed * Time.deltaTime;
        }

        public void SetRudder(float direction)
        {
            _wheelDirection = direction;
        }

        public void SetAnchor(bool dropped)
        {
            _isAnchorDown = dropped;
        }
    }
}