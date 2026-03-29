using PirateSeas.Ocean;
using UnityEngine;
namespace Ship
{
    public class ShipBuoyancy : MonoBehaviour
    {
        [SerializeField] private OceanManager _OceanManager;
        [SerializeField] private Vector3 _FrontOfShip;
        [SerializeField] private Vector3 _RightOfShip;
        [SerializeField] private Vector3 _LeftOfShip;
        [SerializeField] private Vector3 _BackOfShip;
        [SerializeField] private float _HeightOffset = 0.5f;
        [SerializeField] private float _SmoothSpeed = 8f;

        void Update()
        {
            var frontOfShipWorld = transform.TransformPoint(_FrontOfShip);
            var rightOfShipWorld = transform.TransformPoint(_RightOfShip);
            var leftOfShipWorld = transform.TransformPoint(_LeftOfShip);
            var backOfShipWorld = transform.TransformPoint(_BackOfShip);

            var waveDisplacementFront = _OceanManager.GetWaveDisplacementAt(frontOfShipWorld.x, frontOfShipWorld.z);
            var waveDisplacementRight = _OceanManager.GetWaveDisplacementAt(rightOfShipWorld.x, rightOfShipWorld.z);
            var waveDisplacementLeft = _OceanManager.GetWaveDisplacementAt(leftOfShipWorld.x, leftOfShipWorld.z);
            var waveDisplacementBack = _OceanManager.GetWaveDisplacementAt(backOfShipWorld.x, backOfShipWorld.z);

            var targetHeight = ((waveDisplacementFront.y + waveDisplacementRight.y + waveDisplacementLeft.y + waveDisplacementBack.y) / 4) + _HeightOffset;

            Vector3 frontOnWave = new Vector3(frontOfShipWorld.x, waveDisplacementFront.y, frontOfShipWorld.z);
            Vector3 rightOnWave = new Vector3(rightOfShipWorld.x, waveDisplacementRight.y, rightOfShipWorld.z);
            Vector3 leftOnWave = new Vector3(leftOfShipWorld.x, waveDisplacementLeft.y, leftOfShipWorld.z);
            Vector3 backOnWave = new Vector3(backOfShipWorld.x, waveDisplacementBack.y, backOfShipWorld.z);

            Vector3 forward = frontOnWave - backOnWave;
            Vector3 right = rightOnWave - leftOnWave;
            var normalWater = Vector3.Cross(forward, right);

            Quaternion waveRotation = Quaternion.LookRotation(forward, normalWater);

            float shipYaw = transform.eulerAngles.y;
            Vector3 waveEulers = waveRotation.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(waveEulers.x, shipYaw, waveEulers.z);

            float smoothFactor = _SmoothSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothFactor);
            float smoothedHeight = Mathf.Lerp(transform.position.y, targetHeight, smoothFactor);
            transform.position = new Vector3(transform.position.x, smoothedHeight, transform.position.z);
        }
    }
}