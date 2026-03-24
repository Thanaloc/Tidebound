using UnityEngine;
using PirateSeas.Ocean.FFT;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Single entry point for the ocean system.
    /// Supports two modes: Gerstner (CPU, simple) and FFT (GPU, realistic).
    /// 
    /// Hot-reload: click "Regenerate" in the inspector or modify settings 
    /// in the OceanSettings SO and call RegenerateSpectrum() to see changes live.
    /// </summary>
    [RequireComponent(typeof(OceanMeshGenerator))]
    [RequireComponent(typeof(GerstnerWaves))]
    public class OceanManager : MonoBehaviour
    {
        public enum OceanMode { Gerstner, FFT }

        [Header("Mode")]
        [SerializeField] private OceanMode _mode = OceanMode.FFT;

        [Header("Configuration")]
        [SerializeField] private OceanSettings _settings;

        [Header("Rendering")]
        [SerializeField] private Material _oceanMaterial;

        [Header("FFT Compute Shaders")]
        [SerializeField] private ComputeShader _spectrumShader;
        [SerializeField] private ComputeShader _timeSpectrumShader;
        [SerializeField] private ComputeShader _fftButterflyShader;

        [Header("Debug")]
        [Tooltip("Click this in play mode to regenerate the spectrum with current settings.")]
        [SerializeField] private bool _regenerate;

        private OceanMeshGenerator _meshGen;
        private GerstnerWaves _gerstner;
        private OceanFFT _fft;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError("[OceanManager] No OceanSettings assigned!");
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

            if (_mode == OceanMode.Gerstner)
            {
                _gerstner.Initialize(_meshGen, _settings.waves);
            }
            else
            {
                InitializeFFT();
            }
        }

        private void InitializeFFT()
        {
            if (_spectrumShader == null || _timeSpectrumShader == null || _fftButterflyShader == null)
            {
                Debug.LogError("[OceanManager] FFT mode requires all 3 compute shaders assigned!");
                _mode = OceanMode.Gerstner;
                _gerstner.Initialize(_meshGen, _settings.waves);
                return;
            }

            _fft = new OceanFFT(_spectrumShader, _timeSpectrumShader, _fftButterflyShader);
            _fft.Initialize(_settings);
        }

        private void Update()
        {
            if (_mode == OceanMode.Gerstner)
            {
                _gerstner.Tick();
            }
            else if (_fft != null)
            {
                _fft.Update(_settings, Time.time);

                if (_oceanMaterial != null && _fft.HeightMap != null)
                {
                    _oceanMaterial.SetTexture("_HeightMap", _fft.HeightMap);
                    _oceanMaterial.SetTexture("_DisplaceXMap", _fft.DisplaceXMap);
                    _oceanMaterial.SetTexture("_DisplaceZMap", _fft.DisplaceZMap);
                    _oceanMaterial.SetFloat("_MeshSize", _settings.meshSize);
                }
            }
        }

        private void OnDestroy()
        {
            _fft?.Dispose();
        }

        /// <summary>
        /// Regenerates the spectrum with current settings. Can be called at any time.
        /// </summary>
        public void RegenerateSpectrum()
        {
            if (_fft != null)
            {
                _fft.Initialize(_settings);
                Debug.Log("[OceanManager] Spectrum regenerated with current settings.");
            }
        }

        private void PushMaterialProperties()
        {
            if (_oceanMaterial == null) return;
            _oceanMaterial.SetColor("_ShallowColor", _settings.shallowColor);
            _oceanMaterial.SetColor("_DeepColor", _settings.deepColor);
            _oceanMaterial.SetFloat("_FresnelPower", _settings.fresnelPower);
        }

        // ══════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════

        public Vector3 GetWaveDisplacementAt(float x, float z)
        {
            if (_mode == OceanMode.Gerstner)
                return _gerstner.GetDisplacementAt(x, z, Time.time);

            return Vector3.zero;
        }

        public float GetWaveHeightAt(float x, float z)
        {
            return GetWaveDisplacementAt(x, z).y;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Hot-reload: when _regenerate is toggled in the inspector, regenerate the spectrum
            if (_regenerate)
            {
                _regenerate = false;
                if (Application.isPlaying)
                    RegenerateSpectrum();
            }

            if (!Application.isPlaying || _settings == null) return;

            if (_mode == OceanMode.Gerstner && _gerstner != null)
                _gerstner.Initialize(_meshGen, _settings.waves);

            PushMaterialProperties();
        }
#endif
    }
}
