using UnityEngine;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// All ocean parameters live here. Tweak in the inspector, swap presets easily.
    /// Create via: Right-click > PirateSeas > Ocean Settings
    /// </summary>
    [CreateAssetMenu(fileName = "OceanSettings", menuName = "PirateSeas/Ocean Settings")]
    public class OceanSettings : ScriptableObject
    {
        [Header("Mesh")]
        [Tooltip("Vertices per side. 256 for FFT (must be power of 2).")]
        [Range(32, 512)]
        public int meshResolution = 256;

        [Tooltip("Total size of the water plane in world units.")]
        public float meshSize = 200f;

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

        [Header("FFT Ocean")]
        [Tooltip("Texture resolution for FFT. Must be a power of 2. 256 is the sweet spot.")]
        public int fftResolution = 256;

        [Tooltip("How intense the waves are overall.")]
        [Range(0.0001f, 0.01f)]
        public float spectrumScale = 0.0005f;

        [Tooltip("Wind speed in m/s. Higher = bigger waves.")]
        [Range(1f, 40f)]
        public float windSpeed = 15f;

        [Tooltip("Wind direction (gets normalized).")]
        public Vector2 windDirection = new Vector2(1f, 0.5f);

        [Tooltip("How much waves align with wind. 0 = all directions, 6+ = very directional.")]
        [Range(0f, 10f)]
        public float windDependency = 2f;

        [Tooltip("Suppresses tiny waves below this size (meters). Removes high-freq noise.")]
        [Range(0f, 5f)]
        public float smallWaveCutoff = 0.1f;

        [Header("Visual")]
        public Color shallowColor = new Color(0.1f, 0.6f, 0.7f, 0.9f);
        public Color deepColor = new Color(0.02f, 0.1f, 0.2f, 1f);

        [Tooltip("How reflective the water gets at grazing angles.")]
        [Range(0f, 5f)]
        public float fresnelPower = 3f;
    }

    [System.Serializable]
    public struct GerstnerWaveConfig
    {
        [Tooltip("Peak height in meters.")]
        public float amplitude;

        [Tooltip("Distance between two crests in meters.")]
        public float wavelength;

        [Tooltip("How fast the wave travels (m/s).")]
        public float speed;

        [Tooltip("Propagation direction (gets normalized at runtime).")]
        public Vector2 direction;

        [Tooltip("Crest sharpness. 0 = smooth sine, 1 = sharp peak. Above 1 = artifacts.")]
        [Range(0f, 1f)]
        public float steepness;
    }
}