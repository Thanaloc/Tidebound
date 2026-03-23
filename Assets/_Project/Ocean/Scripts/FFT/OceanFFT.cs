using UnityEngine;
using UnityEngine.Rendering;

namespace PirateSeas.Ocean.FFT
{
    /// <summary>
    /// Orchestrates the FFT ocean pipeline: spectrum → time evolution → IFFT → displacement textures.
    /// OceanManager talks to this, never to the individual FFT modules.
    /// </summary>
    public class OceanFFT
    {
        private readonly SpectrumGenerator _spectrum;
        private readonly TimeEvolution _timeEvolution;
        private readonly FFTSolver _fftSolver;

        private RenderTexture _heightMap;
        private RenderTexture _displaceXMap;
        private RenderTexture _displaceZMap;

        public RenderTexture HeightMap => _heightMap;
        public RenderTexture DisplaceXMap => _displaceXMap;
        public RenderTexture DisplaceZMap => _displaceZMap;

        public OceanFFT(ComputeShader spectrumShader, ComputeShader timeShader, ComputeShader fftShader)
        {
            _spectrum = new SpectrumGenerator(spectrumShader);
            _timeEvolution = new TimeEvolution(timeShader);
            _fftSolver = new FFTSolver(fftShader);
        }

        public void Initialize(OceanSettings settings)
        {
            _spectrum.Generate(settings);
        }

        public void Update(OceanSettings settings, float time)
        {
            int size = settings.fftResolution;

            _timeEvolution.Evolve(_spectrum.H0Texture, settings, time);

            _heightMap = _fftSolver.Execute(_timeEvolution.HeightSpectrum, size);
        }

        public void Dispose()
        {
            _spectrum.Dispose();
            _timeEvolution.Dispose();
            _fftSolver.Dispose();
        }
    }
}
