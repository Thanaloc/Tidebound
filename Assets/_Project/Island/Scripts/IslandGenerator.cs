using UnityEngine;

namespace PirateSeas.Island
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class IslandGenerator : MonoBehaviour
    {
        [Header("Noise")]
        [SerializeField] private float _NoiseFloor = 10f;
        [SerializeField] private int _Octaves = 3;
        [SerializeField] private float _BaseFrequency = 0.02f;

        [Header("Island Shape")]
        [SerializeField] private float _MinCenterHeight = 3f;
        [SerializeField] private float _FalloffStrength = 3f;
        [SerializeField] private float _SubmergeOffset = 5f;

        [Header("Shaping")]
        [SerializeField] private float _BeachHeight = 2f;
        [SerializeField] private float _TerraceHeight = 5f;
        [SerializeField][Range(0f, 1f)] private float _TerraceStrength = 0.5f;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private IslandRegistry _islandRegistry;

        private float _seed;
        private float _size;

        private float _innerRadius;
        private float _outerRadius;

        private float _maskNoiseStrength;
        private float _maskNoiseFrequency;

        private int _resolution;

        private float _baseAmplitude;

        private Vector2 _peakOffset;

        public float InnerRadius => _innerRadius;
        public float OuterRadius => _outerRadius;

        private void OnDisable()
        {
            if (_islandRegistry != null)
                _islandRegistry.Unregister(this);
        }

        public void Initialize(float size, float seed, IslandRegistry registry, IslandConfigSO config)
        {
            float t = Mathf.InverseLerp(config.MinSize, config.MaxSize, size);

            _size = size;
            _seed = seed;
            _islandRegistry = registry;


            _peakOffset = new Vector2(Random.Range(-_size * 0.15f, _size * 0.15f), Random.Range(-_size * 0.15f, _size * 0.15f));
            _innerRadius = _size * config.InnerRadiusRatio;
            _outerRadius = _size * config.OuterRadiusRatio;
            _baseAmplitude = _size * config.AmplitudeRatio;
            _SubmergeOffset = _size * config.SubmergeRatio;
            _BaseFrequency = config.FrequencyFactor / _size;
            _NoiseFloor = Mathf.Lerp(config.NoiseFloorMin, config.NoiseFloorMax, t);
            _resolution = _size > 300 ? 256 : 128;
            _maskNoiseStrength = Mathf.Lerp(config.MaskNoiseStrengthMax, config.MaskNoiseStrengthMin, t);
            _maskNoiseFrequency = Mathf.Lerp(config.MaskNoiseFreqMax, config.MaskNoiseFreqMin, t);

            _islandRegistry.Register(this);
            GenerateIsland();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _mesh != null)
                GenerateIsland();
        }
#endif

        private void GenerateIsland()
        {
            _mesh = new Mesh();
            _mesh.name = "IslandMesh";
            if (_resolution > 255)
                _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            GenerateVertices();
            GenerateTriangles();

            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = _mesh;
            var collider = GetComponent<MeshCollider>();
            if (collider == null)
                collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = _mesh;

        }

        private void GenerateVertices()
        {
            int vertCount = (_resolution + 1) * (_resolution + 1);
            _vertices = new Vector3[vertCount];

            float halfSize = _size / 2f;
            float step = _size / _resolution;

            for (int z = 0; z <= _resolution; z++)
            {
                for (int x = 0; x <= _resolution; x++)
                {
                    int index = z * (_resolution + 1) + x;

                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;

                    float height = SampleNoise(worldX, worldZ);
                    float mask = RadialMask(worldX, worldZ, halfSize);

                    float minHeight = mask * _MinCenterHeight;
                    float finalHeight = Mathf.Max(minHeight, height * mask) - _SubmergeOffset;

                    float shapingWeight = Mathf.SmoothStep(-1f, 0.5f, finalHeight);

                    float beachBlend = 1 - Mathf.SmoothStep(0, _BeachHeight, finalHeight);
                    float shaped = Mathf.Lerp(finalHeight, _BeachHeight * 0.3f, beachBlend);

                    float stepped = Mathf.Round(shaped / _TerraceHeight) * _TerraceHeight;
                    shaped = Mathf.Lerp(shaped, stepped, _TerraceStrength);

                    finalHeight = Mathf.Lerp(finalHeight, shaped, shapingWeight);

                    _vertices[index] = new Vector3(worldX, finalHeight, worldZ);
                }
            }
        }

        private float SampleNoise(float x, float z)
        {
            float total = 0f;
            float frequency = _BaseFrequency;
            float amplitude = _baseAmplitude;

            for (int i = 0; i < _Octaves; i++)
            {
                float noise = Mathf.PerlinNoise((x + _seed) * frequency, (z + _seed) * frequency);
                noise = (noise - 0.5f) * 2f * amplitude;
                total += noise;
                frequency *= 2f;
                amplitude /= 2f;
            }

            return Mathf.Max(0f, total + _NoiseFloor);
        }

        private float RadialMask(float x, float z, float halfSize)
        {
            float distance = Mathf.Sqrt((x - _peakOffset.x) * (x - _peakOffset.x) + (z - _peakOffset.y) * (z - _peakOffset.y));
            float noise = Mathf.PerlinNoise((x + _seed) * _maskNoiseFrequency, (z + _seed) * _maskNoiseFrequency) * _maskNoiseStrength;
            distance = Mathf.Clamp01((distance / halfSize) + noise - 0.2f);
            float mask = Mathf.Pow(1 - distance, _FalloffStrength);
            return mask;
        }

        private void GenerateTriangles()
        {
            _triangles = new int[_resolution * _resolution * 6];
            int tri = 0;

            for (int z = 0; z < _resolution; z++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    int current = z * (_resolution + 1) + x;
                    int next = current + _resolution + 1;

                    _triangles[tri++] = current;
                    _triangles[tri++] = next;
                    _triangles[tri++] = current + 1;

                    _triangles[tri++] = current + 1;
                    _triangles[tri++] = next;
                    _triangles[tri++] = next + 1;
                }
            }
        }
    }
}