using UnityEngine;

namespace PirateSeas.Ocean.FFT
{
    /// <summary>
    /// Runs the Inverse FFT on a complex spectrum texture to produce spatial-domain output.
    /// 
    /// Normalization: 1/N after horizontal passes + 1/N after vertical passes = 1/N² total.
    /// Output values are in physically meaningful units (meters of displacement).
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

        private RenderTexture[] _outputs;
        private int _outputIndex;

        public FFTSolver(ComputeShader shader, int outputCount = 3)
        {
            _shader = shader;
            _kernelBitRevH = _shader.FindKernel("BitReversalHorizontal");
            _kernelBitRevV = _shader.FindKernel("BitReversalVertical");
            _kernelButterflyH = _shader.FindKernel("ButterflyHorizontal");
            _kernelButterflyV = _shader.FindKernel("ButterflyVertical");

            _outputs = new RenderTexture[outputCount];
            _outputIndex = 0;
        }

        public RenderTexture Execute(RenderTexture spectrumInput, int size)
        {
            EnsureBuffers(size);

            int logN = (int)Mathf.Log(size, 2);
            int groups = Mathf.CeilToInt(size / 8f);

            _shader.SetInt("_Resolution", size);
            _shader.SetInt("_LogN", logN);

            RenderTexture current;
            RenderTexture next;

            // ── Horizontal: bit-reverse rows, then butterfly ──

            _shader.SetTexture(_kernelBitRevH, "_Input", spectrumInput);
            _shader.SetTexture(_kernelBitRevH, "_Output", _bufferA);
            _shader.Dispatch(_kernelBitRevH, groups, groups, 1);

            current = _bufferA;
            next = _bufferB;

            for (int pass = 0; pass < logN; pass++)
            {
                _shader.SetInt("_Pass", pass);
                _shader.SetBool("_LastPass", false);

                _shader.SetTexture(_kernelButterflyH, "_Input", current);
                _shader.SetTexture(_kernelButterflyH, "_Output", next);
                _shader.Dispatch(_kernelButterflyH, groups, groups, 1);

                (current, next) = (next, current);
            }

            // ── Vertical: bit-reverse columns, then butterfly ──

            _shader.SetTexture(_kernelBitRevV, "_Input", current);
            _shader.SetTexture(_kernelBitRevV, "_Output", next);
            _shader.Dispatch(_kernelBitRevV, groups, groups, 1);

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

            // Copy to persistent output
            EnsureOutput(size);
            Graphics.CopyTexture(current, _outputs[_outputIndex]);
            RenderTexture result = _outputs[_outputIndex];

            _outputIndex = (_outputIndex + 1) % _outputs.Length;

            return result;
        }

        private void EnsureBuffers(int size)
        {
            if (_bufferA != null && _bufferA.width == size) return;

            if (_bufferA != null) _bufferA.Release();
            if (_bufferB != null) _bufferB.Release();

            _bufferA = CreateBuffer(size, true);
            _bufferB = CreateBuffer(size, true);
        }

        private void EnsureOutput(int size)
        {
            if (_outputs[_outputIndex] != null && _outputs[_outputIndex].width == size) return;

            if (_outputs[_outputIndex] != null) _outputs[_outputIndex].Release();

            _outputs[_outputIndex] = CreateBuffer(size, false);
        }

        private RenderTexture CreateBuffer(int size, bool randomWrite)
        {
            var tex = new RenderTexture(size, size, 0, RenderTextureFormat.RGFloat)
            {
                enableRandomWrite = randomWrite,
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

            for (int i = 0; i < _outputs.Length; i++)
            {
                if (_outputs[i] != null) _outputs[i].Release();
            }
        }
    }
}
