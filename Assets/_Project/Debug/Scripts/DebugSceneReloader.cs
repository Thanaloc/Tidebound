using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugScripts
{
    public class DebugSceneReloader : MonoBehaviour
    {
        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
            inputActions.Gameplay.Reload.performed += ctx => ReloadScene();
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
