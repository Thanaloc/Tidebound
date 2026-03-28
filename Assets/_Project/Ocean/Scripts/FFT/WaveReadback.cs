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

            Vector2 uv = new Vector2(u, v) * _textureResolution;

            int px = (int)((uv.x % _textureResolution) + _textureResolution) % _textureResolution;
            int py = (int)((uv.y % _textureResolution) + _textureResolution) % _textureResolution;

            int index = (py * _textureResolution + px) * 2;

            Vector3 displacement = new Vector3(_displaceXArray[index] * choppyStrength, _heightArray[index] * displacementStrength, _displaceZArray[index] * choppyStrength);

            return displacement;
        }
    }
}
