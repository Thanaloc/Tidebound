using UnityEngine;

namespace PirateSeas.Ocean.FFT
{
    public class JacobianComputer
    {
        private readonly ComputeShader _shader;
        private readonly int _kernelId;

        private RenderTexture _jacobianMap;

        public RenderTexture JacobianMap => _jacobianMap;

        public JacobianComputer(ComputeShader shader)
        {
            _shader = shader;
            _kernelId = _shader.FindKernel("ComputeJacobian");
        }

        public void Compute(RenderTexture displaceX, RenderTexture displaceZ, int resolution, float choppyStrength, float meshSize)
        {
            float texel = meshSize / resolution;

            _shader.SetInt("_Resolution", resolution);
            _shader.SetFloat("_ChoppyStrength", choppyStrength);
            _shader.SetFloat("_TexelSize", texel);

            EnsureTextures(resolution);

            // Output
            _shader.SetTexture(_kernelId, "_JacobianMap", _jacobianMap);
            _shader.SetTexture(_kernelId, "_DisplaceXMap", displaceX);
            _shader.SetTexture(_kernelId, "_DisplaceZMap", displaceZ);

            int groups = Mathf.CeilToInt(resolution / 8f);
            _shader.Dispatch(_kernelId, groups, groups, 1);
        }

        private void EnsureTextures(int size)
        {
            EnsureTexture(ref _jacobianMap, size);
        }

        private void EnsureTexture(ref RenderTexture tex, int size)
        {
            if (tex != null && tex.width == size) return;
            if (tex != null) tex.Release();

            tex = new RenderTexture(size, size, 0, RenderTextureFormat.RFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.Create();
        }

        public void Dispose()
        {
            if (_jacobianMap != null) _jacobianMap.Release();
        }
    }
}
