using UnityEngine;
using PirateSeas.Ocean.FFT;
using Ocean;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Single entry point for the ocean system.
    /// 
    /// Visual params (colors, fresnel, etc.) are on the Material — edit there.
    /// Simulation params (wind, spectrum) are on the OceanSettings SO.
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
        [SerializeField] private ComputeShader _jacobianShader;

        [Header("Tiling")]
        [SerializeField] private OceanTiling _OceanTiling;
        [SerializeField] private Transform _Follower;

        [Header("Debug")]
        [Tooltip("Toggle in play mode to regenerate spectrum with current SO values.")]
        [SerializeField] private bool _regenerate;

        private OceanMeshGenerator _meshGen;
        private GerstnerWaves _gerstner;
        private OceanFFT _fft;
        private WaveReadback _waveReadback;

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
                GetComponent<MeshRenderer>().material = _oceanMaterial;

            if (_mode == OceanMode.Gerstner)
            {
                _gerstner.Initialize(_meshGen, _settings.waves);
            }
            else
            {
                InitializeFFT();
            }

            _OceanTiling.Initialize(_settings.meshSize);
            _OceanTiling.CreateTiles(_meshGen.Mesh, _oceanMaterial);
            GetComponent<MeshRenderer>().enabled = false;
        }

        private void InitializeFFT()
        {
            if (_spectrumShader == null || _timeSpectrumShader == null || _fftButterflyShader == null)
            {
                Debug.LogError("[OceanManager] FFT mode requires all 3 compute shaders!");
                _mode = OceanMode.Gerstner;
                _gerstner.Initialize(_meshGen, _settings.waves);
                return;
            }

            _fft = new OceanFFT(_spectrumShader, _timeSpectrumShader, _jacobianShader, _fftButterflyShader);
            _fft.Initialize(_settings);
            _waveReadback = new WaveReadback(_settings.fftResolution, _settings.meshSize);
        }

        private void Update()
        {
            if (_mode == OceanMode.Gerstner)
            {
                _gerstner.Tick();
            }
            else if (_fft != null)
            {
                _fft.Update(_settings, Time.time * _settings.timeScale, _oceanMaterial.GetFloat("_ChoppyStrength"));

                if (_oceanMaterial != null && _fft.HeightMap != null)
                {
                    _oceanMaterial.SetTexture("_HeightMap", _fft.HeightMap);
                    _oceanMaterial.SetTexture("_DisplaceXMap", _fft.DisplaceXMap);
                    _oceanMaterial.SetTexture("_DisplaceZMap", _fft.DisplaceZMap);
                    _oceanMaterial.SetTexture("_JacobianMap", _fft.JacobianMap);
                    _oceanMaterial.SetFloat("_MeshSize", _settings.meshSize);

                    _waveReadback.RequestReadback(_fft.HeightMap, _fft.DisplaceXMap, _fft.DisplaceZMap);
                }

                _OceanTiling.UpdateTiling(_Follower.position);
            }
        }

        private void OnDestroy()
        {
            _fft?.Dispose();
        }

        public void RegenerateSpectrum()
        {
            if (_fft != null)
            {
                _fft.Initialize(_settings);
                Debug.Log("[OceanManager] Spectrum regenerated.");
            }
        }

        // ══════════════════════════════════════════════════════
        // PUBLIC API
        // ══════════════════════════════════════════════════════

        public Vector3 GetWaveDisplacementAt(float x, float z)
        {
            if (_mode == OceanMode.Gerstner)
                return _gerstner.GetDisplacementAt(x, z, Time.time);

            if (_waveReadback == null)
                return Vector3.zero;

            return _waveReadback.GetDisplacement(new Vector3(x, 0, z), _oceanMaterial.GetFloat("_DisplacementStrength"), _oceanMaterial.GetFloat("_ChoppyStrength"));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_regenerate)
            {
                _regenerate = false;
                if (Application.isPlaying)
                    RegenerateSpectrum();
            }

            if (!Application.isPlaying || _settings == null) return;

            if (_mode == OceanMode.Gerstner && _gerstner != null)
                _gerstner.Initialize(_meshGen, _settings.waves);
        }
#endif
    }
}
