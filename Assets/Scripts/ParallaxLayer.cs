using UnityEngine;

namespace Platformer.View
{
    [ExecuteAlways]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Vector2 movementScale = Vector2.one;
        [SerializeField] private bool lockY = true;
        [SerializeField] private bool snapToPixels = true;
        [SerializeField] private int pixelsPerUnit = 100;

        private Transform _cam;
        private Vector3 _initialOffset;
        private Vector3 _lastCamPos;
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
            _cam = Camera.main ? Camera.main.transform : null;
            _initialOffset = transform.position - (_cam ? _cam.position : Vector3.zero);
            if (_cam != null) _lastCamPos = _cam.position;
        }

        private void LateUpdate()
        {
            if (!_cam) return;

            // Delta-based movement (use double to reduce precision loss)
            var dx = (double)_cam.position.x - _lastCamPos.x;
            var dy = (double)_cam.position.y - _lastCamPos.y;

            var delta = new Vector3((float)(dx * movementScale.x), (float)(dy * movementScale.y), 0f);

            // Apply delta relative to current position (preserves continuity with fast movement)
            var target = transform.position + delta;

            // Enforce initial offset on Z and optional locking of Y
            target.z = transform.position.z;
            if (lockY)
                target.y = _initialOffset.y;

            // Pixel snapping
            if (snapToPixels && _camera && _camera.orthographic)
            {
                var unitsPerPixel = 1f / pixelsPerUnit;
                target.x = Mathf.Round(target.x / unitsPerPixel) * unitsPerPixel;
                target.y = Mathf.Round(target.y / unitsPerPixel) * unitsPerPixel;
            }

            transform.position = target;
            _lastCamPos = _cam.position;
        }
    }
}