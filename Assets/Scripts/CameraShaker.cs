using UnityEngine;
using Unity.Cinemachine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private float shakeForce = 1f;
    
    public static CameraShaker instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    internal void CameraShake(CinemachineImpulseSource impulseSource)
    {
        impulseSource.GenerateImpulseWithForce(shakeForce);
    }
}
