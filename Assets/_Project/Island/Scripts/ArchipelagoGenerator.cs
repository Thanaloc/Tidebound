using Ship;
using UnityEngine;

namespace PirateSeas.Island
{
    public class ArchipelagoGenerator : MonoBehaviour
    {
        [SerializeField] private IslandRegistry _IslandRegistry;
        [SerializeField] private IslandGenerator _IslandPrefab;
        [SerializeField] private int _IslandNumber = 8;
        [SerializeField] private float _MinIslandSize = 150;
        [SerializeField] private float _MaxIslandSize = 300;
        [SerializeField] private float _DistanceBetweenCircles = 500;
        [SerializeField] private float _Jitter = 50;

        [SerializeField] private Transform _IslandsParent;

        [SerializeField] private IslandConfigSO _IslandConfig;

        [SerializeField] private Transform _PlayerRef;
        [SerializeField] private Transform _BoatRef;

        [SerializeField] private ShipMovement _ShipMovement;

        private void Start()
        {
            float spawnIslandSize = _MaxIslandSize;

            SpawnIsland(Vector3.zero, spawnIslandSize);

            _BoatRef.transform.position = new Vector3(spawnIslandSize * 0.45f, 0f, 0f);
            _ShipMovement.SetAnchor(true);

            Vector3 rayOrigin = new Vector3(0f, 200f, 0f);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 400f))
                _PlayerRef.transform.position = hit.point + Vector3.up * 1f;

            int remaining = _IslandNumber - 1;
            int islandIndex = 0;
            int ring = 1;

            while (islandIndex < remaining)
            {
                int islandsOnRing = ring + 2;
                float ringRadius = _DistanceBetweenCircles * ring;
                float angleStep = 360f / islandsOnRing;

                for (int i = 0; i < islandsOnRing && islandIndex < remaining; i++)
                {
                    float angle = (angleStep * i) + Random.Range(-_Jitter, _Jitter);
                    float radius = ringRadius + Random.Range(-_Jitter, _Jitter);

                    float angleRad = angle * Mathf.Deg2Rad;
                    float x = radius * Mathf.Cos(angleRad);
                    float z = radius * Mathf.Sin(angleRad);

                    float size = Random.Range(_MinIslandSize, _MaxIslandSize);
                    SpawnIsland(new Vector3(x, 0f, z), size);

                    islandIndex++;
                }

                ring++;
            }
        }

        private void SpawnIsland(Vector3 position, float size)
        {
            IslandGenerator island = Instantiate(_IslandPrefab, _IslandsParent);
            island.transform.position = position;
            island.Initialize(size, Random.Range(0f, 10000f), _IslandRegistry, _IslandConfig);
        }
    }
}

