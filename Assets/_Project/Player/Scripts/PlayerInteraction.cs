using FPSController;
using Player;
using Ship;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform _PlayerCam;
    [SerializeField] private InputActionReference _InteractActionRef;

    [SerializeField] private PlayerMotor _PlayerMotor;
    [SerializeField] private PlayerInputHandler _PlayerInputHandler;
    [SerializeField] private ShipPassenger _ShipPassenger;

    public PlayerMotor PlayerMotor { get { return _PlayerMotor; } }
    public ShipPassenger ShipPassenger { get { return _ShipPassenger; } }
    public Vector2 MoveInput { get { return _PlayerInputHandler.MoveInput; } }

    private LayerMask _interactableLayerMask;
    private IInteractable _currentLookedInteractable = null;
    private IInteractable _currentCapturedInteractable = null;

    private void Start()
    {
        _interactableLayerMask = LayerMask.GetMask("Interactable");
        _InteractActionRef.action.Enable();
    }

    private void OnDisable()
    {
        _InteractActionRef.action.Disable();
    }

    private void Update()
    {
        if (_InteractActionRef.action.WasPressedThisFrame())
        {
            if (_currentCapturedInteractable != null)
            {
                _currentCapturedInteractable.OnInteractionTriggered(this);
                _currentCapturedInteractable = null;
            }
            else if (_currentLookedInteractable != null)
            {
                _currentLookedInteractable.OnInteractionTriggered(this);

                if (_currentLookedInteractable.HoldInteraction)
                    _currentCapturedInteractable = _currentLookedInteractable;
            }
        }

        if (_currentCapturedInteractable != null)
            return;

        RaycastHit hit;

        if (Physics.SphereCast(_PlayerCam.position, 0.4f, _PlayerCam.forward, out hit, 3.5f, _interactableLayerMask))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();

#if UNITY_EDITOR
            Debug.DrawRay(_PlayerCam.position, _PlayerCam.forward * hit.distance, Color.green);
            Debug.Log("Did Hit");
#endif

            if (interactable.Equals(_currentLookedInteractable))
                return;

            if (_currentLookedInteractable != null)
                _currentLookedInteractable.OnRaycastHitExit();

            interactable.OnRaycastHitEnter();
            _currentLookedInteractable = interactable;
        }
        else
        {
#if UNITY_EDITOR
            Debug.DrawRay(_PlayerCam.position, _PlayerCam.forward * 1000, Color.white);
            Debug.Log("Did not Hit");
#endif

            if (_currentLookedInteractable != null)
            {
                _currentLookedInteractable.OnRaycastHitExit();
                _currentLookedInteractable = null;
            }
        }
    }
}