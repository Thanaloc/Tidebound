using UnityEngine;

namespace PirateSeas.Island
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class IslandGenerator : MonoBehaviour
    {
        [Header("Mesh")]
        [SerializeField] private int _Resolution = 128;
        [SerializeField] private float _Size = 200f;

        [Header("Noise")]
        [SerializeField] private float _NoiseFloor = 10f;
        [SerializeField] private float _Seed = 0f;
        [SerializeField] private int _Octaves = 3;
        [SerializeField] private float _BaseFrequency = 0.02f;
        [SerializeField] private float _BaseAmplitude = 30f;

        [Header("Island Shape")]
        [SerializeField] private float _MinCenterHeight = 3f;
        [SerializeField] private float _FalloffStrength = 3f;
        [SerializeField] private float _SubmergeOffset = 5f;

        [Header("Ocean Attenuation")]
        [SerializeField] private Material _OceanMaterial;
        [SerializeField] private float _InnerRadius = 40f;
        [SerializeField] private float _OuterRadius = 80f;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;

        private void Start()
        {
            _Seed = Random.Range(0f, 10000f);
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
            if (_Resolution > 255)
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

            if (_OceanMaterial != null)
            {
                _OceanMaterial.SetVector("_IslandCenter", transform.position);
                _OceanMaterial.SetFloat("_IslandInnerRadius", _InnerRadius);
                _OceanMaterial.SetFloat("_IslandOuterRadius", _OuterRadius);
            }
        }

        private void GenerateVertices()
        {
            int vertCount = (_Resolution + 1) * (_Resolution + 1);
            _vertices = new Vector3[vertCount];

            float halfSize = _Size / 2f;
            float step = _Size / _Resolution;

            for (int z = 0; z <= _Resolution; z++)
            {
                for (int x = 0; x <= _Resolution; x++)
                {
                    int index = z * (_Resolution + 1) + x;

                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;

                    float height = SampleNoise(worldX, worldZ);
                    float mask = RadialMask(worldX, worldZ, halfSize);

                    float minHeight = mask * _MinCenterHeight;
                    float finalHeight = Mathf.Max(minHeight, height * mask) - _SubmergeOffset;

                    _vertices[index] = new Vector3(worldX, finalHeight, worldZ);
                }
            }
        }

        private float SampleNoise(float x, float z)
        {
            float total = 0f;
            float frequency = _BaseFrequency;
            float amplitude = _BaseAmplitude;

            for (int i = 0; i < _Octaves; i++)
            {
                float noise = Mathf.PerlinNoise((x + _Seed) * frequency, (z + _Seed) * frequency);
                noise = (noise - 0.5f) * 2f * amplitude;
                total += noise;
                frequency *= 2f;
                amplitude /= 2f;
            }

            return Mathf.Max(0f, total + _NoiseFloor);
        }

        private float RadialMask(float x, float z, float halfSize)
        {
            float distance = Mathf.Sqrt(x * x + z * z);
            float noise = Mathf.PerlinNoise((x + _Seed) * 0.03f, (z + _Seed) * 0.03f) * 0.4f;
            distance = Mathf.Clamp01((distance / halfSize) + noise - 0.2f);
            float mask = Mathf.Pow(1 - distance, _FalloffStrength);
            return mask;
        }

        private void GenerateTriangles()
        {
            _triangles = new int[_Resolution * _Resolution * 6];
            int tri = 0;

            for (int z = 0; z < _Resolution; z++)
            {
                for (int x = 0; x < _Resolution; x++)
                {
                    int current = z * (_Resolution + 1) + x;
                    int next = current + _Resolution + 1;

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