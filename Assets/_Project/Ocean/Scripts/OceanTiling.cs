using System;
using UnityEngine;

namespace Ocean
{
    public class OceanTiling : MonoBehaviour
    {
        [SerializeField] private Transform _TilesParent;

        private GameObject[] _tiles;
        private float _meshSize;
        private Vector3 _anchor;

        private int _numberOfTiles = 9;

        public void Initialize(float meshSize)
        {
            _tiles = new GameObject[_numberOfTiles];
            _meshSize = meshSize;
        }

        public void CreateTiles(Mesh mesh, Material mat)
        {
            for (int i = 0; i < _numberOfTiles; i++)
            {
                GameObject tile = new GameObject($"Tile{i}");
                tile.transform.parent = _TilesParent;
                tile.AddComponent<MeshFilter>().sharedMesh = mesh;
                tile.AddComponent<MeshRenderer>().sharedMaterial = mat;
                _tiles[i] = tile;
            }

            PlaceTilesAroundAnchor();
        }

        private void PlaceTilesAroundAnchor()
        {
            int counter = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    _tiles[counter].transform.position = new Vector3(_anchor.x + j * _meshSize, 0, _anchor.z + i * _meshSize);
                    counter++;
                }
            }
        }

        public void UpdateTiling(Vector3 followerPosition)
        {
            Vector3 newAnchor = new Vector3(Mathf.Round(followerPosition.x / _meshSize) * _meshSize, 0, Mathf.Round(followerPosition.z / _meshSize) * _meshSize);

            if (newAnchor == _anchor)
                return;

            _anchor = newAnchor;
            PlaceTilesAroundAnchor();
        }
    }
}

