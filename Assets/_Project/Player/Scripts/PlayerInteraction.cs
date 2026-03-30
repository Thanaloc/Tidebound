using Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform _PlayerCam;
    [SerializeField] private InputActionReference _InteractActionRef;

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
                _currentCapturedInteractable.OnInteractionTriggered();
                _currentCapturedInteractable = null;
            }
            else if (_currentLookedInteractable != null)
            {
                _currentLookedInteractable.OnInteractionTriggered();

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