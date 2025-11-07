using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePointTransform;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private PlayerBehaviour player;
    
    [Space]
    [Header("Settings")]
    
    private InputAction _fireAction;
    
    private void Awake()
    {
        _fireAction = InputSystem.actions.FindAction("Player/Attack");
    }

    private void OnEnable()
    {
        _fireAction.Enable();
        _fireAction.performed += OnShoot;
    }

    private void OnDisable()
    {
        _fireAction.Disable();
        _fireAction.performed -= OnShoot;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        Instantiate(bulletPrefab, firePointTransform.position, firePointTransform.rotation);
    }
}
