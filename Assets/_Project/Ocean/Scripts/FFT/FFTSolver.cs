using UnityEngine;

namespace PirateSeas.Ocean.FFT
{
    /// <summary>
    /// Runs the Inverse FFT on a complex spectrum texture to produce spatial-domain output.
    /// 
    /// Pure math tool — knows nothing about oceans.
    /// 
    /// Pipeline per axis:
    ///   1. Bit-reversal permutation (reorder input)
    ///   2. log2(N) butterfly passes (combine pairs)
    ///   3. Result: spatial-domain displacement values
    /// 
    /// Uses ping-pong: two textures, alternate read/write each pass.
    /// </summary>
    public class FFTSolver
    {
        private readonly ComputeShader _shader;

        private readonly int _kernelBitRevH;
        private readonly int _kernelBitRevV;
        private readonly int _kernelButterflyH;
        private readonly int _kernelButterflyV;

        private RenderTexture _bufferA;
        private RenderTexture _bufferB;

        public FFTSolver(ComputeShader shader)
        {
            _shader = shader;
            _kernelBitRevH = _shader.FindKernel("BitReversalHorizontal");
            _kernelBitRevV = _shader.FindKernel("BitReversalVertical");
            _kernelButterflyH = _shader.FindKernel("ButterflyHorizontal");
            _kernelButterflyV = _shader.FindKernel("ButterflyVertical");
        }

        /// <summary>
        /// Runs the full 2D IFFT. Returns the texture containing the spatial result.
        /// The input texture is not modified.
        /// </summary>
        public RenderTexture Execute(RenderTexture spectrumInput, int size)
        {
            EnsureBuffers(size);

            int logN = (int)Mathf.Log(size, 2);
            int groups = Mathf.CeilToInt(size / 8f);

            _shader.SetInt("_Resolution", size);
            _shader.SetInt("_LogN", logN);

            // We track which buffer holds the "current" data.
            // After each dispatch, we swap.
            RenderTexture current;
            RenderTexture next;

            // ── Horizontal: bit-reverse rows, then butterfly ──

            _shader.SetTexture(_kernelBitRevH, "_Input", spectrumInput);
            _shader.SetTexture(_kernelBitRevH, "_Output", _bufferA);
            _shader.Dispatch(_kernelBitRevH, groups, groups, 1);

            // After bit-reversal, current data is in _bufferA
            current = _bufferA;
            next = _bufferB;

            for (int pass = 0; pass < logN; pass++)
            {
                _shader.SetInt("_Pass", pass);
                _shader.SetBool("_LastPass", pass == logN - 1);

                _shader.SetTexture(_kernelButterflyH, "_Input", current);
                _shader.SetTexture(_kernelButterflyH, "_Output", next);
                _shader.Dispatch(_kernelButterflyH, groups, groups, 1);

                // Swap
                (current, next) = (next, current);
            }

            // After horizontal passes, result is in "current"

            // ── Vertical: bit-reverse columns, then butterfly ──

            _shader.SetTexture(_kernelBitRevV, "_Input", current);
            _shader.SetTexture(_kernelBitRevV, "_Output", next);
            _shader.Dispatch(_kernelBitRevV, groups, groups, 1);

            // Swap after bit-reversal
            (current, next) = (next, current);

            for (int pass = 0; pass < logN; pass++)
            {
                _shader.SetInt("_Pass", pass);
                _shader.SetBool("_LastPass", pass == logN - 1);

                _shader.SetTexture(_kernelButterflyV, "_Input", current);
                _shader.SetTexture(_kernelButterflyV, "_Output", next);
                _shader.Dispatch(_kernelButterflyV, groups, groups, 1);

                (current, next) = (next, current);
            }

            // "current" now holds the final spatial-domain result
            return current;
        }

        private void EnsureBuffers(int size)
        {
            if (_bufferA != null && _bufferA.width == size) return;

            if (_bufferA != null) _bufferA.Release();
            if (_bufferB != null) _bufferB.Release();

            _bufferA = CreateBuffer(size);
            _bufferB = CreateBuffer(size);
        }

        private RenderTexture CreateBuffer(int size)
        {
            var tex = new RenderTexture(size, size, 0, RenderTextureFormat.RGFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.Create();
            return tex;
        }

        public void Dispose()
        {
            if (_bufferA != null) _bufferA.Release();
            if (_bufferB != null) _bufferB.Release();
        }
    }
}
