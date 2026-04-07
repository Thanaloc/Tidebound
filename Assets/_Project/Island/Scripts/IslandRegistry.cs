using System.Collections.Generic;
using UnityEngine;

namespace PirateSeas.Island
{
    public class IslandRegistry : MonoBehaviour
    {
        [SerializeField] private Material _OceanMat;

        private Vector4[] _islandCenters;
        private float[] _islandInnerRadii;
        private float[] _islandOuterRadii;

        private List<IslandGenerator> _islands;
        private int _count = 0;

        private void Awake()
        {
            _islands = new List<IslandGenerator>();
            _islandCenters = new Vector4[16];
            _islandInnerRadii = new float[16];
            _islandOuterRadii = new float[16];
        }

        public void Register(IslandGenerator p_island)
        {
            if (!_islands.Contains(p_island))
                _islands.Add(p_island);

            PushToShader();
        }

        public void Unregister(IslandGenerator p_island)
        {
            if (_islands.Contains(p_island))
                _islands.Remove(p_island);

            PushToShader();
        }

        private void PushToShader()
        {
            _count = Mathf.Min(_islands.Count, 16);

            for (int i = 0; i < _count; i++)
            {
                _islandCenters[i] = _islands[i].transform.position;
                _islandInnerRadii[i] = _islands[i].InnerRadius;
                _islandOuterRadii[i] = _islands[i].OuterRadius;
            }

            _OceanMat.SetInt("_IslandCount", _count);
            _OceanMat.SetVectorArray("_IslandCenters", _islandCenters);
            _OceanMat.SetFloatArray("_IslandInnerRadii", _islandInnerRadii);
            _OceanMat.SetFloatArray("_IslandOuterRadii", _islandOuterRadii);
        }

        public float GetAttenuationAt(float x, float z)
        {
            float attenuation = 1f;

            for (int i = 0; i < _count; i++)
            {
                float dist = Mathf.Sqrt(
                    (x - _islandCenters[i].x) * (x - _islandCenters[i].x) +
                    (z - _islandCenters[i].z) * (z - _islandCenters[i].z)
                );

                float att = Mathf.SmoothStep(0f, 1f,
                    (dist - _islandInnerRadii[i]) / (_islandOuterRadii[i] - _islandInnerRadii[i])
                );

                attenuation = Mathf.Min(attenuation, att);
            }

            return Mathf.Max(0.15f, attenuation);
        }
    }
}