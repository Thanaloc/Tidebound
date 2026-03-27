using UnityEngine;

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
        private readonly JacobianComputer _jacobianComputer;

        private RenderTexture _heightMap;
        private RenderTexture _displaceXMap;
        private RenderTexture _displaceZMap;

        public RenderTexture HeightMap => _heightMap;
        public RenderTexture DisplaceXMap => _displaceXMap;
        public RenderTexture DisplaceZMap => _displaceZMap;
        public RenderTexture JacobianMap => _jacobianComputer.JacobianMap;

        public OceanFFT(ComputeShader spectrumShader, ComputeShader timeShader, ComputeShader jacobianShader, ComputeShader fftShader)
        {
            _spectrum = new SpectrumGenerator(spectrumShader);
            _timeEvolution = new TimeEvolution(timeShader);
            _jacobianComputer = new JacobianComputer(jacobianShader);
            _fftSolver = new FFTSolver(fftShader, 3); // 3 output textures
        }

        public void Initialize(OceanSettings settings)
        {
            _spectrum.Generate(settings);
        }

        public void Update(OceanSettings settings, float time, float choppyStrength)
        {
            int size = settings.fftResolution;

            // Step 1: Animate all 3 spectra
            _timeEvolution.Evolve(_spectrum.H0Texture, settings, time);

            // Step 2: IFFT each channel — each call gets its own output texture
            _heightMap = _fftSolver.Execute(_timeEvolution.HeightSpectrum, size);
            _displaceXMap = _fftSolver.Execute(_timeEvolution.DisplaceXSpectrum, size);
            _displaceZMap = _fftSolver.Execute(_timeEvolution.DisplaceZSpectrum, size);

            _jacobianComputer.Compute(_displaceXMap, _displaceZMap, settings.fftResolution, choppyStrength, settings.meshSize);
        }

        public void Dispose()
        {
            _spectrum.Dispose();
            _timeEvolution.Dispose();
            _fftSolver.Dispose();
            _jacobianComputer.Dispose();
        }
    }
}