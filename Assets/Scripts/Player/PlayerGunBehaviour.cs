using System;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunBehaviour : MonoBehaviour
{
    /// <summary>
    /// This script handles gun logic and ammo.
    /// </summary>
    
    private static readonly int Shoot = Animator.StringToHash("shoot");
    private static readonly int Reload = Animator.StringToHash("reload");
    
    [Header("Shooting References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePointTransform;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject userInterfaceObject;
    [SerializeField] private Texture2D aimCursor;
    
    [Space]
    [Header("Settings")]
    
    [Tooltip("Cooldown for shooting in seconds")]
    [SerializeField] private float shootCooldown = 0.4f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed aim.")]
    [SerializeField] private float gunAimCooldown = 1f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed reload.")]
    [SerializeField] private float reloadCooldown = 1.5f;
    
    [Tooltip("Maximum amount of bullets possible in chamber")]
    [SerializeField] private int maxAmmo = 3;
    
    [Tooltip("Starting ammo")]
    [SerializeField] private int startingAmmo = 2;
    
    [Space]
    [Header("Audio Sources")]
    [SerializeField] private AudioSource[] sounds;
    
    [Space]
    [Header("Debug Values")]
    [SerializeField] internal int currentChamberedAmmo;
    [SerializeField] private float nextShootTime;
    [SerializeField] private float nextHolsterTime;
    [SerializeField] private int totalOwnedAmmo;
    [SerializeField] internal bool isAiming = false;

    private PlayerController _playerScript;
    
    private InputAction _fireAction;
    private InputAction _reloadAction;
    internal InputAction _aimAction;

    private PlayerUI _countingUI;
    
    private void Awake()
    {
        _playerScript = player.GetComponent<PlayerController>();
        _fireAction = InputSystem.actions.FindAction("Player/Attack");
        _reloadAction = InputSystem.actions.FindAction("Player/Reload");
        _aimAction =  InputSystem.actions.FindAction("Player/Aim");
        currentChamberedAmmo = startingAmmo;
        totalOwnedAmmo = startingAmmo;
        
        _countingUI = userInterfaceObject.GetComponent<PlayerUI>();
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);
    }

    private void Start()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_playerScript.velocity.x != 0 || _playerScript.velocity.y != 0)
        {
            isAiming = false;
        }
        Cursor.visible = isAiming;
    }

    private void OnEnable()
    {
        _fireAction.Enable();
        _fireAction.performed += OnShoot;
        
        _reloadAction.Enable();
        _reloadAction.performed += OnReload;
        
        _aimAction.Enable();
        _aimAction.performed += OnAim;
    }

    private void OnDisable()
    {
        _fireAction.Disable();
        _fireAction.performed -= OnShoot;
        
        _reloadAction.Disable();
        _reloadAction.performed -= OnReload;
        
        _aimAction.Disable();
        _aimAction.performed -= OnAim;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!isAiming)
        {
            TriggerSoundAndAnimation(0);
            return;
        }
        
        if (Time.time >= nextShootTime && currentChamberedAmmo > 0)
        {
            Instantiate(bulletPrefab, firePointTransform.position, firePointTransform.rotation);
            currentChamberedAmmo--;
            totalOwnedAmmo--;
            nextShootTime = Time.time + shootCooldown; // Set the next shoot time

            _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);

            StartCoroutine(LockMovementForDuration(shootCooldown));
            StartCoroutine(LockJumpForDuration(shootCooldown));
            
            TriggerSoundAndAnimation(1);
            
            Debug.Log("Bullet count:"  + currentChamberedAmmo);
        }
        else if (currentChamberedAmmo == 0)
        {
            Debug.Log("Out of Ammo! Reloading automatically.");

            OnGunEmpty();
        }
    }

    private System.Collections.IEnumerator LockMovementForDuration(float duration)
    {
        _playerScript.moveAction.Disable();
        yield return new WaitForSeconds(duration);
        _playerScript.moveAction.Enable();
    }
    
    private System.Collections.IEnumerator LockJumpForDuration(float duration)
    {
        _playerScript.jumpAction.Disable();
        yield return new WaitForSeconds(duration);
        _playerScript.jumpAction.Enable();
    }
    
    private System.Collections.IEnumerator LockShootForDuration(float duration)
    {
        _fireAction.Disable();
        yield return new WaitForSeconds(duration);
        _fireAction.Enable();
    }
    
    private void OnReload(InputAction.CallbackContext context)
    {
        if (!isAiming)
        {
            TriggerSoundAndAnimation(0);
            return;
        }
        
        InitReload();
    }
    
    private void InitReload()
    {
        TriggerSoundAndAnimation(2);

        StartCoroutine(LockMovementForDuration(reloadCooldown));
        StartCoroutine(LockJumpForDuration(reloadCooldown));
        StartCoroutine(LockShootForDuration(reloadCooldown));
        
        
        switch (currentChamberedAmmo)
        {
            case 0:
                // Chamber is empty, fill it with the maximum allowed amount or what the player has
                currentChamberedAmmo = Mathf.Min(maxAmmo, totalOwnedAmmo);
                break;
            default:
                if (currentChamberedAmmo > 0)
                {
                    // If there's ammo already in the chamber, reload but don't exceed total owned or max ammo
                    currentChamberedAmmo = Mathf.Min(currentChamberedAmmo + maxAmmo, maxAmmo, totalOwnedAmmo);
                }
                else
                {
                    Debug.LogError("Problem with ammo. Check if negative.");
                }
                break;
        }
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);
    }

    private void OnGunEmpty()
    {
        TriggerSoundAndAnimation(3);
        InitReload();
    }
    
    private void TriggerSoundAndAnimation(int fxNum)
    {
        switch (fxNum)
        {
            case 0:
                // not aiming, cannot reload or shoot
                sounds[0].Play();
                break;
            case 1:
                // shoot
                sounds[1].Play();
                player.GetComponent<Animator>().SetTrigger(Shoot);
                break;
            case 2:
                // reload
                sounds[2].Play();
                player.GetComponent<Animator>().SetTrigger(Reload);
                break;
            case 3:
                // empty magazine
                sounds[3].Play();
                break;
        }
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        isAiming = !isAiming;
        Cursor.SetCursor(aimCursor, Vector2.zero, default);
        
        StartCoroutine(LockMovementForDuration(gunAimCooldown));
        StartCoroutine(LockJumpForDuration(gunAimCooldown));
        
        _playerScript.PlaySoundAndAnims(5);
    }

    internal void OnAmmoPickedUp(int ammoNum)
    {
        totalOwnedAmmo += ammoNum;
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);
    }
}
