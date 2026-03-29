using UnityEngine;

namespace Ship
{
    public class ShipMovement : MonoBehaviour
    {
        [SerializeField] private float _MaxSpeed = 9f;
        [SerializeField] private float _AccelRatio = .007f;
        [SerializeField] private float _DeccelRatio = .025f;
        [SerializeField] private float _RotationSpeed = 20f;

        private float _currentSpeed = 0;
        private float _aimedSpeed = 0;
        private float _wheelDirection = 0;
        public bool _isAnchorDown = false;

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

            transform.position += transform.forward * _currentSpeed * Time.deltaTime;
        }

        public void SetRudder(float direction)
        {

        }

        public void SetAnchor (bool dropped)
        {
            _isAnchorDown = dropped;
        }
    }
}

