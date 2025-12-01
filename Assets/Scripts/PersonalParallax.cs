using UnityEngine;

public class PersonalParallax : MonoBehaviour
{
    [SerializeField] private Vector3 movementScale = new (1f,0f,0.5f);
    
    private Transform _camera;
    private Vector3 _lastCamPos;

    private void Awake()
    {
        if (Camera.main != null) 
            _camera = Camera.main.transform;
        _lastCamPos = _camera.position;
    }

    private void FixedUpdate()
    {
        // use doubles for intermediate calc to reduce precision loss at large coords
        var camDx = (double)_camera.position.x - _lastCamPos.x;
        var camDy = (double)_camera.position.y - _lastCamPos.y;
        var camDz = (double)_camera.position.z - _lastCamPos.z;

        transform.position += new Vector3(
            (float)(camDx * movementScale.x),
            (float)(camDy * movementScale.y),
            (float)(camDz * movementScale.z)
        );

        _lastCamPos = _camera.position;
    }
}
