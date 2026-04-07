using UnityEngine;
using UnityEngine.Rendering;

namespace PirateSeas.Ocean.FFT
{
    public class WaveReadback
    {
        private int _textureResolution;

        private float[] _heightArray;
        private float[] _displaceXArray;
        private float[] _displaceZArray;

        private int _pendingReadbacks = 0;

        private float _meshSize;

        public WaveReadback(int textureResolution, float meshSize)
        {
            _textureResolution = textureResolution;
            _meshSize = meshSize;

            int size = _textureResolution * _textureResolution * 2;

            _heightArray = new float[size];
            _displaceXArray = new float[size];
            _displaceZArray = new float[size];
        }

        public void RequestReadback(RenderTexture height, RenderTexture displaceX, RenderTexture displaceZ)
        {
            if (_pendingReadbacks != 0)
                return;

            _pendingReadbacks = 3;

            AsyncGPUReadback.Request(height, 0, OnReadbackCompleteHeight);
            AsyncGPUReadback.Request(displaceX, 0, OnReadbackCompleteDisplaceX);
            AsyncGPUReadback.Request(displaceZ, 0, OnReadbackCompleteDisplaceZ);
        }

        private void OnReadbackCompleteHeight(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                _pendingReadbacks--;
                return;
            }

            var data = request.GetData<float>();
            data.CopyTo(_heightArray);

            _pendingReadbacks--;
        }

        private void OnReadbackCompleteDisplaceX(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                _pendingReadbacks--;
                return;
            }

            var data = request.GetData<float>();
            data.CopyTo(_displaceXArray);

            _pendingReadbacks--;
        }

        private void OnReadbackCompleteDisplaceZ(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                _pendingReadbacks--;
                return;
            }

            var data = request.GetData<float>();
            data.CopyTo(_displaceZArray);

            _pendingReadbacks--;
        }

        public Vector3 GetDisplacement(Vector3 position, float displacementStrength, float choppyStrength)
        {
            float u = (position.x / _meshSize) + 0.5f;
            float v = (position.z / _meshSize) + 0.5f;

            float fx = (u * _textureResolution) % _textureResolution;
            float fz = (v * _textureResolution) % _textureResolution;

            // Wrap to positive range
            fx = ((fx % _textureResolution) + _textureResolution) % _textureResolution;
            fz = ((fz % _textureResolution) + _textureResolution) % _textureResolution;

            int x0 = (int)fx;
            int z0 = (int)fz;
            int x1 = (x0 + 1) % _textureResolution;
            int z1 = (z0 + 1) % _textureResolution;

            float tx = fx - x0;
            float tz = fz - z0;

            float h00 = SampleHeight(x0, z0);
            float h10 = SampleHeight(x1, z0);
            float h01 = SampleHeight(x0, z1);
            float h11 = SampleHeight(x1, z1);
            float height = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz) * displacementStrength;

            float dx00 = SampleDisplaceX(x0, z0);
            float dx10 = SampleDisplaceX(x1, z0);
            float dx01 = SampleDisplaceX(x0, z1);
            float dx11 = SampleDisplaceX(x1, z1);
            float displaceX = Mathf.Lerp(Mathf.Lerp(dx00, dx10, tx), Mathf.Lerp(dx01, dx11, tx), tz) * choppyStrength;

            float dz00 = SampleDisplaceZ(x0, z0);
            float dz10 = SampleDisplaceZ(x1, z0);
            float dz01 = SampleDisplaceZ(x0, z1);
            float dz11 = SampleDisplaceZ(x1, z1);
            float displaceZ = Mathf.Lerp(Mathf.Lerp(dz00, dz10, tx), Mathf.Lerp(dz01, dz11, tx), tz) * choppyStrength;

            return new Vector3(displaceX, height, displaceZ);
        }

        private float SampleHeight(int x, int z)
        {
            return _heightArray[(z * _textureResolution + x) * 2];
        }

        private float SampleDisplaceX(int x, int z)
        {
            return _displaceXArray[(z * _textureResolution + x) * 2];
        }

        private float SampleDisplaceZ(int x, int z)
        {
            return _displaceZArray[(z * _textureResolution + x) * 2];
        }
    }
}
