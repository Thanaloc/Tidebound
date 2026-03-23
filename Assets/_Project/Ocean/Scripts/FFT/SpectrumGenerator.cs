using UnityEngine;

namespace PirateSeas.Ocean.FFT
{
    /// <summary>
    /// Dispatches the InitialSpectrum compute shader on the GPU.
    /// Produces h0(k) — the initial frequency-domain representation of the ocean.
    /// 
    /// This only runs once at startup (or when wind parameters change).
    /// </summary>
    public class SpectrumGenerator
    {
        private readonly ComputeShader _shader;
        private readonly int _kernelId;

        private RenderTexture _h0Texture;

        // Expose the UAV texture directly — TimeSpectrum reads it as RWTexture2D
        public RenderTexture H0Texture => _h0Texture;

        public SpectrumGenerator(ComputeShader shader)
        {
            _shader = shader;
            _kernelId = _shader.FindKernel("GenerateInitialSpectrum");
        }

        public void Generate(OceanSettings settings)
        {
            int size = settings.fftResolution;

            EnsureTexture(ref _h0Texture, size);

            _shader.SetInt("_Resolution", size);
            _shader.SetFloat("_MeshSize", settings.meshSize);
            _shader.SetFloat("_SpectrumScale", settings.spectrumScale);
            _shader.SetFloat("_WindSpeed", settings.windSpeed);
            _shader.SetVector("_WindDirection", settings.windDirection.normalized);
            _shader.SetFloat("_WindDependency", settings.windDependency);
            _shader.SetFloat("_SmallWaveCutoff", settings.smallWaveCutoff);

            _shader.SetTexture(_kernelId, "_H0Texture", _h0Texture);

            int groups = Mathf.CeilToInt(size / 8f);
            _shader.Dispatch(_kernelId, groups, groups, 1);
        }

        private void EnsureTexture(ref RenderTexture tex, int size)
        {
            if (tex != null && tex.width == size) return;
            if (tex != null) tex.Release();

            tex = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.Create();
        }

        public void Dispose()
        {
            if (_h0Texture != null) _h0Texture.Release();
        }
    }
}
