using UnityEngine;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Single entry point for the ocean system. Wires up sub-modules, owns the config.
    /// 
    /// Scene setup:
    ///   1. Create a GameObject "Ocean"
    ///   2. Add OceanManager (other components get added automatically)
    ///   3. Assign an OceanSettings asset in the inspector
    ///   4. Assign a Material using the PirateSeas/OceanSurface shader
    ///   5. Hit Play
    /// </summary>
    [RequireComponent(typeof(OceanMeshGenerator))]
    [RequireComponent(typeof(GerstnerWaves))]
    public class OceanManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OceanSettings _settings;

        [Header("Rendering")]
        [SerializeField] private Material _oceanMaterial;

        private OceanMeshGenerator _meshGen;
        private GerstnerWaves _gerstner;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("[OceanManager] No OceanSettings assigned! Drag one into the inspector.");
                enabled = false;
                return;
            }

            _meshGen = GetComponent<OceanMeshGenerator>();
            _gerstner = GetComponent<GerstnerWaves>();

            _meshGen.GenerateMesh(_settings.meshResolution, _settings.meshSize);

            if (_oceanMaterial != null)
            {
                GetComponent<MeshRenderer>().material = _oceanMaterial;
                PushMaterialProperties();
            }

            _gerstner.Initialize(_meshGen, _settings.waves);
        }

        private void Update()
        {
            _gerstner.Tick();
        }

        /// <summary>
        /// Sends colors and params from the SO to the shader.
        /// Called at startup and whenever we need a refresh (e.g. weather change).
        /// </summary>
        private void PushMaterialProperties()
        {
            if (_oceanMaterial == null) return;

            _oceanMaterial.SetColor("_ShallowColor", _settings.shallowColor);
            _oceanMaterial.SetColor("_DeepColor", _settings.deepColor);
            _oceanMaterial.SetFloat("_FresnelPower", _settings.fresnelPower);
        }

        // ══════════════════════════════════════════════════════
        // PUBLIC API
        // Other systems go through here, never touch sub-modules directly.
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Returns wave displacement at a world position. Used by ship, floating objects, etc.
        /// </summary>
        public Vector3 GetWaveDisplacementAt(float x, float z)
        {
            return _gerstner.GetDisplacementAt(x, z, Time.time);
        }

        /// <summary>
        /// Shorthand — just the Y height at a world position.
        /// </summary>
        public float GetWaveHeightAt(float x, float z)
        {
            return GetWaveDisplacementAt(x, z).y;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Hot-reload: tweak settings in play mode and see changes live
            if (Application.isPlaying && _settings != null)
            {
                if (_gerstner != null)
                    _gerstner.Initialize(_meshGen, _settings.waves);

                PushMaterialProperties();
            }
        }
#endif
    }
}
