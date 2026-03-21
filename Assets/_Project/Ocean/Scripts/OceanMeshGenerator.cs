using UnityEngine;

namespace PirateSeas.Ocean
{
    /// <summary>
    /// Builds a flat grid mesh at runtime with enough vertices for smooth wave displacement.
    /// Unity's default quad has 4 verts — we need 128x128 = 16K for the waves to look good.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class OceanMeshGenerator : MonoBehaviour
    {
        private Mesh _mesh;
        private Vector3[] _baseVertices;      // flat grid, never changes
        private Vector3[] _displacedVertices; // updated every frame by GerstnerWaves

        /// <summary>Flat grid positions. GerstnerWaves reads these as starting points.</summary>
        public Vector3[] BaseVertices => _baseVertices;

        /// <summary>Wave-displaced positions. GerstnerWaves writes here, then we push to the mesh.</summary>
        public Vector3[] DisplacedVertices => _displacedVertices;

        public Mesh Mesh => _mesh;

        /// <summary>
        /// Builds the grid. Called once at startup by OceanManager.
        /// </summary>
        public void GenerateMesh(int resolution, float size)
        {
            _mesh = new Mesh
            {
                name = "OceanMesh",
                // UInt16 caps at 65K verts (255x255). Switch to UInt32 for bigger grids.
                indexFormat = resolution > 255
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            int vertCount = resolution * resolution;
            _baseVertices = new Vector3[vertCount];
            _displacedVertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            float halfSize = size * 0.5f;
            float step = size / (resolution - 1);

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * resolution + x;
                    _baseVertices[i] = new Vector3(
                        -halfSize + x * step,
                        0f,
                        -halfSize + z * step
                    );
                    uvs[i] = new Vector2((float)x / (resolution - 1), (float)z / (resolution - 1));
                }
            }

            // Two triangles per grid cell
            int cellCount = (resolution - 1) * (resolution - 1);
            var triangles = new int[cellCount * 6];
            int t = 0;

            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = z * resolution + x;

                    triangles[t++] = i;
                    triangles[t++] = i + resolution;
                    triangles[t++] = i + 1;

                    triangles[t++] = i + 1;
                    triangles[t++] = i + resolution;
                    triangles[t++] = i + resolution + 1;
                }
            }

            _mesh.vertices = _baseVertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            // Start with the flat grid as initial state
            System.Array.Copy(_baseVertices, _displacedVertices, _baseVertices.Length);

            GetComponent<MeshFilter>().mesh = _mesh;
        }

        /// <summary>
        /// Pushes displaced vertices into the mesh. Called every frame after GerstnerWaves does its thing.
        /// </summary>
        public void ApplyDisplacement()
        {
            _mesh.vertices = _displacedVertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }
    }
}
