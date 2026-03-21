using UnityEngine;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Applies Gerstner wave displacement to the ocean mesh every frame.
    /// 
    /// THE MATH — GERSTNER WAVES (aka Trochoidal Waves)
    /// 
    /// Unlike a basic sine wave (Y only), Gerstner moves vertices on all 3 axes,
    /// creating sharp crests and round troughs — like real ocean waves.
    /// 
    /// For a vertex at base position (x₀, 0, z₀), the displaced position is:
    /// 
    ///   x = x₀ - Σ [ Q × A × D.x × sin(dot + phase) ]
    ///   y =      Σ [ A × cos(dot + phase) ]
    ///   z = z₀ - Σ [ Q × A × D.y × sin(dot + phase) ]
    /// 
    /// Where:
    ///   Σ       = sum over all wave layers
    ///   A       = amplitude
    ///   D       = normalized propagation direction (Vector2)
    ///   Q       = steepness (0 = smooth sine, 1 = sharp crest)
    ///   k       = 2π / wavelength (spatial frequency)
    ///   phase   = speed × k × time (temporal frequency)
    ///   dot     = k × (D · position) — dot product of direction with vertex XZ
    /// </summary>
    public class GerstnerWaves : MonoBehaviour
    {
        private struct CachedWaveVariables
        {
            public float k;
            public float phase;
            public Vector2 dir;

            public CachedWaveVariables(float k, float phase, Vector2 dir)
            {
                this.k = k;
                this.phase = phase;
                this.dir = dir;
            }
        }

        private OceanMeshGenerator _meshGen;
        private GerstnerWaveConfig[] _waves;
        private CachedWaveVariables[] _wavesCachedVariables;


        /// <summary>
        /// Called once by OceanManager at startup.
        /// </summary>
        public void Initialize(OceanMeshGenerator meshGen, GerstnerWaveConfig[] waves)
        {
            _meshGen = meshGen;
            _waves = waves;
            _wavesCachedVariables = new CachedWaveVariables[waves.Length];
        }

        /// <summary>
        /// Called every frame by OceanManager.
        /// </summary>
        public void Tick()
        {
            if (_meshGen == null || _waves == null) return;

            EvaluateWaves(Time.time);
            _meshGen.ApplyDisplacement();
        }

        /// <summary>
        /// Core displacement loop.
        /// </summary>
        private void EvaluateWaves(float time)
        {
            Vector3[] baseVerts = _meshGen.BaseVertices;
            Vector3[] displaced = _meshGen.DisplacedVertices;

            for (int w = 0; w < _waves.Length; w++)
            {
                float k = 2 * Mathf.PI / _waves[w].wavelength;
                float phase = _waves[w].speed * k * time;
                Vector2 dir = _waves[w].direction.normalized;

                _wavesCachedVariables[w] = new CachedWaveVariables(k, phase, dir);
            }

            for (int i = 0; i < baseVerts.Length; i++)
            {
                Vector3 basePos = baseVerts[i];

                float sumX = 0f;
                float sumY = 0f;
                float sumZ = 0f;

                for (int w = 0; w < _waves.Length; w++)
                {
                    GerstnerWaveConfig currentWaveConfig = _waves[w];
                    CachedWaveVariables currentWaveVariables = _wavesCachedVariables[w];
                    float dot = currentWaveVariables.k * ((currentWaveVariables.dir.x * basePos.x) + (currentWaveVariables.dir.y * basePos.z));

                    sumX += -(currentWaveConfig.steepness * currentWaveConfig.amplitude * currentWaveVariables.dir.x * Mathf.Sin(dot + currentWaveVariables.phase));
                    sumY += currentWaveConfig.amplitude * Mathf.Cos(dot + currentWaveVariables.phase);
                    sumZ += -(currentWaveConfig.steepness * currentWaveConfig.amplitude * currentWaveVariables.dir.y * Mathf.Sin(dot + currentWaveVariables.phase));
                }

                displaced[i] = new Vector3(
                    basePos.x + sumX,
                    sumY,
                    basePos.z + sumZ
                );
            }
        }

        /// <summary>
        /// Returns wave displacement at a single world position.
        /// Used by the ship for buoyancy (Phase 2).
        /// </summary>
        public Vector3 GetDisplacementAt(float x, float z, float time)
        {
            // TODO (Phase 2)
            return Vector3.zero;
        }
    }
}