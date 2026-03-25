using UnityEngine;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Ocean SIMULATION parameters only. Visual params live on the Material.
    /// Create via: Right-click > PirateSeas > Ocean Settings
    /// </summary>
    [CreateAssetMenu(fileName = "OceanSettings", menuName = "PirateSeas/Ocean Settings")]
    public class OceanSettings : ScriptableObject
    {
        [Header("Mesh")]
        [Tooltip("Vertices per side. 256 for FFT (must be power of 2).")]
        [Range(32, 512)]
        public int meshResolution = 256;

        [Tooltip("Total size of the water plane in world units. 500 = good vertex density.")]
        public float meshSize = 500f;

        [Header("Gerstner Waves (fallback)")]
        public GerstnerWaveConfig[] waves = new GerstnerWaveConfig[]
        {
            new GerstnerWaveConfig
            {
                amplitude = 1.0f,
                wavelength = 40f,
                speed = 8f,
                direction = new Vector2(1f, 0f),
                steepness = 0.4f
            },
            new GerstnerWaveConfig
            {
                amplitude = 0.5f,
                wavelength = 20f,
                speed = 5f,
                direction = new Vector2(0.7f, 0.7f),
                steepness = 0.3f
            },
            new GerstnerWaveConfig
            {
                amplitude = 0.25f,
                wavelength = 10f,
                speed = 3f,
                direction = new Vector2(-0.3f, 0.8f),
                steepness = 0.5f
            }
        };

        [Header("FFT Spectrum")]
        public int fftResolution = 256;

        [Tooltip("Overall wave energy.")]
        [Range(0.00001f, 0.01f)]
        public float spectrumScale = 0.0005f;

        [Tooltip("Wind speed in m/s.")]
        [Range(1f, 40f)]
        public float windSpeed = 20f;

        [Tooltip("Wind direction (gets normalized).")]
        public Vector2 windDirection = new Vector2(1f, 0f);

        [Tooltip("Directional spread. 0 = all directions, 6+ = very directional.")]
        [Range(0f, 10f)]
        public float windDependency = 4f;

        [Tooltip("Suppresses wavelengths below this (meters).")]
        [Range(0f, 5f)]
        public float smallWaveCutoff = 0.5f;
    }

    [System.Serializable]
    public struct GerstnerWaveConfig
    {
        public float amplitude;
        public float wavelength;
        public float speed;
        public Vector2 direction;
        [Range(0f, 1f)]
        public float steepness;
    }
}
