using UnityEngine;

namespace PirateSeas.Ocean.FFT
{
    /// <summary>
    /// Dispatches the TimeSpectrum compute shader every frame.
    /// Takes the static h0(k) and produces animated h(k,t) spectra for height, X and Z.
    /// </summary>
    public class TimeEvolution
    {
        private readonly ComputeShader _shader;
        private readonly int _kernelId;

        private RenderTexture _heightSpectrum;
        private RenderTexture _displaceXSpectrum;
        private RenderTexture _displaceZSpectrum;

        public RenderTexture HeightSpectrum => _heightSpectrum;
        public RenderTexture DisplaceXSpectrum => _displaceXSpectrum;
        public RenderTexture DisplaceZSpectrum => _displaceZSpectrum;

        public TimeEvolution(ComputeShader shader)
        {
            _shader = shader;
            _kernelId = _shader.FindKernel("EvolveSpectrum");
        }

        /// <summary>
        /// Runs every frame. Animates the spectrum based on current time.
        /// </summary>
        public void Evolve(RenderTexture h0Texture, OceanSettings settings, float time)
        {
            int size = settings.fftResolution;

            EnsureTextures(size);

            _shader.SetInt("_Resolution", size);
            _shader.SetFloat("_MeshSize", settings.meshSize);
            _shader.SetFloat("_Time", time);

            // Input
            _shader.SetTexture(_kernelId, "_H0Texture", h0Texture);

            // Outputs
            _shader.SetTexture(_kernelId, "_HeightSpectrum", _heightSpectrum);
            _shader.SetTexture(_kernelId, "_DisplaceXSpectrum", _displaceXSpectrum);
            _shader.SetTexture(_kernelId, "_DisplaceZSpectrum", _displaceZSpectrum);

            int groups = Mathf.CeilToInt(size / 8f);
            _shader.Dispatch(_kernelId, groups, groups, 1);
        }

        private void EnsureTextures(int size)
        {
            // These only need 2 floats per pixel (complex number = real + imag)
            EnsureTexture(ref _heightSpectrum, size);
            EnsureTexture(ref _displaceXSpectrum, size);
            EnsureTexture(ref _displaceZSpectrum, size);
        }

        private void EnsureTexture(ref RenderTexture tex, int size)
        {
            if (tex != null && tex.width == size) return;
            if (tex != null) tex.Release();

            tex = new RenderTexture(size, size, 0, RenderTextureFormat.RGFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.Create();
        }

        public void Dispose()
        {
            if (_heightSpectrum != null) _heightSpectrum.Release();
            if (_displaceXSpectrum != null) _displaceXSpectrum.Release();
            if (_displaceZSpectrum != null) _displaceZSpectrum.Release();
        }
    }
}
